#include "contiki.h"
#include "net/rime/rime.h"
#include "net/rime/mesh.h"
#include "cpu.h"
#include "dev/uart.h"

#include "dev/serial-line.h"
#include "dev/sys-ctrl.h"
#include "sys/etimer.h"
#include "sys/rtimer.h"

#include <stdio.h>
#include <string.h>

#define RECEIVED "Rec"
#define NUMBER_OF_NODES 4
#define NUMBER_OF_SENSORS 4*NUMBER_OF_NODES

static struct mesh_conn mesh;
uint8_t address;
uint8_t i;
uint8_t j;
static struct etimer et;

// CC2538 aka The MainFrame, nodeID = 0x0001
/*---------------------------------------------------------------------------*/
PROCESS(example_mesh_process, "Mesh example");
AUTOSTART_PROCESSES(&example_mesh_process);
/*---------------------------------------------------------------------------*/
static void
sent(struct mesh_conn *c)
{
  printf("packet sent\r\n");
}

static void
timedout(struct mesh_conn *c)
{
  printf("packet timedout\r\n");
}

void
PutInArray(uint8_t addr, uint8_t* data, uint8_t hops)
{
  printf("%d %d ", addr, hops);
  for(i=0; i<3; i++)
      {
        printf("%d ", data[i]);
      }
  printf("%d\r\n", data[3]);
}

//Received packet, print out data to terminal
static void
recv(struct mesh_conn *c, const linkaddr_t *from, uint8_t hops)
{
  // Calculate address as one int number
  address = from->u8[0]*10+from->u8[1];
  PutInArray(address,packetbuf_dataptr(), hops);
}

const static struct mesh_callbacks callbacks = {recv, sent, timedout};
/*---------------------------------------------------------------------------*/
PROCESS_THREAD(example_mesh_process, ev, data)
{ 
  PROCESS_EXITHANDLER(mesh_close(&mesh);)
  PROCESS_BEGIN();
    
  // Enter the mesh network and discover routes
  mesh_open(&mesh, 132, &callbacks);

  // Endless loop
  while(1)
  {
     etimer_set(&et, 15* CLOCK_SECOND);
     PROCESS_YIELD();
     etimer_reset(&et);      
  }
  PROCESS_END();
}