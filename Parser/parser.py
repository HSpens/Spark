import serial
import requests
import json
import argparse
from requests.auth import HTTPBasicAuth
from serial.serialutil import SerialException

# Parking ID
PARKING_ID = 1

BAUDRATE = 115200

# Baisc Auth credentials
USER = 'user'
PASS = 'password'

# URLs
sensor_url = 'website/sensor'
parking_url = 'website/parking'

# Parse port
argparser = argparse.ArgumentParser(description='Parse node data.')
argparser.add_argument('port')
args = argparser.parse_args()
PORT = args.port

def createSensorEntry(sensor_id, node_id, sensor_status):
	if (node_id>4):
		print("NODE ID > 4!!!")

	occupied = False
	faulty = False
	if (sensor_status == 9):
		faulty = True
	else:
		occupied = bool(sensor_status)

	data = {
		'id': sensor_id,
		'occupied': occupied,
		'node': node_id,
		'parking': PARKING_ID,
		'faulty': faulty
	}

	return(data)

def startParsing(conn):

	mem = {}

	while True:
		msg = conn.readline().split()

		#print(msg)

		# Unpack message
		node_addr, *data = msg
		node_id = int(node_addr)-1
		nhops, *sensor_data = data
		sensor_array = [int(sensor) for sensor in sensor_data]

		outdata = []

		# Initialize mem
		if (str(node_id) not in mem):
			print('Initial update for node', node_id)
			print()
			mem[str(node_id)] = sensor_array
			for idx, sensor_status in enumerate(sensor_array):
				data = createSensorEntry(idx+1, node_id, sensor_status)
				outdata.append(data)
		else:
			print('Node:', node_id)
			print('Sensors:', sensor_array)
			print('Hops:', int(nhops))
			print()

			# Create sensor entries for updated sensors
			for idx, sensor_status in enumerate(sensor_array):
				if (sensor_status != mem[str(node_id)][idx]):
					#print('Node', node_id, ', sensor', idx+1, 'was updated')
					data = createSensorEntry(idx+1, node_id, sensor_status)
					outdata.append(data)

			# Save new reading in
			mem[str(node_id)] = sensor_array

		# POST data if any
		if (len(outdata) > 0):
			#print(mem)
			r = requests.post(sensor_url, json = outdata, auth=HTTPBasicAuth(USER, PASS))

try:
	conn = serial.Serial(port=PORT, baudrate=BAUDRATE)
	startParsing(conn)
except SerialException as s:
	print('Port is not available.')
