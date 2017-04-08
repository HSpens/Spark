#include "contiki.h"
#include "net/rime/rime.h"
#include "net/rime/mesh.h"
#include "cpu.h"
#include "sys/etimer.h"
#include "sys/rtimer.h"
#include "dev/leds.h"
#include "dev/uart.h"
#include "dev/cc2538-sensors.h"

#include "dev/als-sensor.h"
#include "dev/watchdog.h"
#include "dev/serial-line.h"
#include "dev/sys-ctrl.h"
#include "dev/gpio.h"

#include <stdio.h>
#include <string.h>

#define MESSAGE "Hello"
#define MESSAGE1 "REC"
#define TRIG_PIN_1 1 //PB1 sensor 1, SmartRF06 RF1.5
#define ECHO_PIN_1 3 //PC3 sensor 1, SmartRF06 RF1.4
#define TRIG_PIN_2 5 //PB5 sensor 2, SmartRF06 RF1.17
#define ECHO_PIN_2 5 //PC5 sensor 2, SmartRF06 RF1.8
#define TRIG_PIN_3 2 //PB2 sensor 3, SmartRF06 RF1.11
#define ECHO_PIN_3 6 //PC6 sensor 3, SmartRF06 RF1.10
#define TRIG_PIN_4 3 //PB3 sensor 4, SmartRF06 RF1.13
#define ECHO_PIN_4 7 //PC7 sensor 4, SmartRF06 RF1.12

#define NUMBER_OF_SENSORS 4
#define POLL_INT 5 //loop interval (sec between measurements)
#define THRESHOLD 20 //threashold for when a parking space is 
// considered occupied/availabe (cm)

static struct mesh_conn mesh;
static rtimer_clock_t rt_start, rt_end;
static struct etimer et;
static uint8_t sensor_info[4]; //Binary representation of sensor 
// state /occupied/availabe
static uint8_t sensor_info_old[4];
/*------------------------------------------------------------*/
PROCESS(rime_mesh_process, "Rime-mesh process");
AUTOSTART_PROCESSES(&rime_mesh_process);
/*------------------------------------------------------------*/
static void sent(struct mesh_conn *c) {
  printf("packet sent\r\n");
}

static void timedout(struct mesh_conn *c) {
  printf("packet timedout\r\n");
}

static void recv(struct mesh_conn *c, const linkaddr_t *from, uint8_t hops) {
  printf("Data received from %d.%d: %u (%d)\r\n",
	from->u8[0], from->u8[1],
	*(uint8_t *)packetbuf_dataptr(), packetbuf_datalen());
  printf("Number of hops: %d\r\n", hops);
  if (packetbuf_datalen() != 3) {
    packetbuf_copyfrom(MESSAGE1, strlen(MESSAGE1));
    mesh_send(&mesh, from);
  }
}

static void gpio_config(uint8_t echo_mask, uint8_t trig_mask) { 
  GPIO_SOFTWARE_CONTROL(GPIO_C_BASE, GPIO_PIN_MASK(echo_mask));
  GPIO_SET_INPUT(GPIO_C_BASE, GPIO_PIN_MASK(echo_mask));
  GPIO_SOFTWARE_CONTROL(GPIO_B_BASE, GPIO_PIN_MASK(trig_mask));
  GPIO_SET_OUTPUT(GPIO_B_BASE, GPIO_PIN_MASK(trig_mask));
}

static unsigned long 
measure_distance(uint8_t echo_mask, uint8_t trig_mask) {
  unsigned long distance_cm = 0; 

  //Initialize new measurement
  GPIO_CLR_PIN(GPIO_B_BASE, GPIO_PIN_MASK(trig_mask));
  clock_delay_usec(4);
  GPIO_SET_PIN(GPIO_B_BASE, GPIO_PIN_MASK(trig_mask));
  clock_delay_usec(10);
  GPIO_CLR_PIN(GPIO_B_BASE, GPIO_PIN_MASK(trig_mask));
  
  // Previous ping hasn't finished, abort.
  if (GPIO_READ_PIN(GPIO_C_BASE, GPIO_PIN_MASK(echo_mask))) {
    printf("<An error has occured>\r\n");
    return 9999; // return 9999, error code (might be a broken sensor?)
  }
  
  // Wait for ping to start
  while(!GPIO_READ_PIN(GPIO_C_BASE, GPIO_PIN_MASK(echo_mask))) {   
    // If pin is low, it may enter an infinit loop??         
  }
  rt_start = RTIMER_NOW();
  
  // Wait for ping to stop
  while(GPIO_READ_PIN(GPIO_C_BASE, GPIO_PIN_MASK(echo_mask))) {            
  }
  rt_end = RTIMER_NOW();
  distance_cm = 1000000*(rt_end - rt_start)/RTIMER_SECOND/58;
  
  // Print measured distance 
  printf("Measured distance (cm) on PIN (PB%d, PC%d): %lu\r\n", 
  trig_mask, echo_mask, distance_cm);    
  return distance_cm;
}

static uint8_t occupancy(uint8_t echo_mask, uint8_t trig_mask) {
  uint16_t dist;
  dist = measure_distance(echo_mask, trig_mask);
  if(dist <= THRESHOLD) return 1;
  else if(dist == 9999) return 9; // As an error code (might be a broken sensor)
  else return 0;
}

const static struct mesh_callbacks callbacks = {recv, sent, timedout};
/*---------------------------------------------------------------------------*/
PROCESS_THREAD(rime_mesh_process, ev, data) {
  PROCESS_EXITHANDLER(mesh_close(&mesh);)
  PROCESS_BEGIN();  
  
  // Configure pins with pre-defined pin masks
  gpio_config(ECHO_PIN_1, TRIG_PIN_1); 
  gpio_config(ECHO_PIN_2, TRIG_PIN_2);
  gpio_config(ECHO_PIN_3, TRIG_PIN_3);
  gpio_config(ECHO_PIN_4, TRIG_PIN_4);
  
  // Open rime-mesh on channel 132
  mesh_open(&mesh, 132, &callbacks);

  // Send all sensor readings to central node (addr 1) on start up 
  linkaddr_t addr;

  // Add small delay on start up
  etimer_set(&et, 5 * CLOCK_SECOND);
  PROCESS_YIELD();
  etimer_reset(&et);

  sensor_info_old[0] = occupancy(ECHO_PIN_1, TRIG_PIN_1);
  sensor_info_old[1] = occupancy(ECHO_PIN_2, TRIG_PIN_2);
  sensor_info_old[2] = occupancy(ECHO_PIN_3, TRIG_PIN_3);
  sensor_info_old[3] = occupancy(ECHO_PIN_4, TRIG_PIN_4);
  packetbuf_copyfrom(&sensor_info_old, sizeof(sensor_info_old));
  addr.u8[0] = 0;
  addr.u8[1] = 1; 
  mesh_send(&mesh, &addr);

  // Enter normal running mode, keep reading sensors and sending information
  while(1) {          
    // Measure distance with all sensors and add 0/1 to an array     
    sensor_info[0] = occupancy(ECHO_PIN_1, TRIG_PIN_1);
    sensor_info[1] = occupancy(ECHO_PIN_2, TRIG_PIN_2);
    sensor_info[2] = occupancy(ECHO_PIN_3, TRIG_PIN_3);
    sensor_info[3] = occupancy(ECHO_PIN_4, TRIG_PIN_4);

    for(int i = 0; i<NUMBER_OF_SENSORS; i++) {
      if(sensor_info[i] != sensor_info_old[i]) {
        // If something has changes compared with the old values we
        // send a new distance measurement in cm
        printf("Try to send to ONE\r\n");
        packetbuf_copyfrom(&sensor_info, sizeof(sensor_info));
        addr.u8[0] = 0;
        addr.u8[1] = 1; 
        mesh_send(&mesh, &addr);
        for(int j = 0; j<NUMBER_OF_SENSORS; j++) {
          printf("%u", sensor_info[j]);
        }
        printf("\r\n");
        break;                      
      }
    }
    
    // Set current sensor reading as old sensor reading
    memcpy(sensor_info_old, sensor_info, sizeof(sensor_info)); 
    printf("\r\n");
      
    // Delay measurement loop with pre-defined number of seconds (POLL_INT) 
    etimer_set(&et, POLL_INT * CLOCK_SECOND);
    PROCESS_YIELD();
    etimer_reset(&et);   
  }
  PROCESS_END();
}
/*---------------------------------------------------------------------------*/