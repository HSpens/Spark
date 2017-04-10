package db

import(

	// Standard library packages
	"database/sql"
	"time"

	// Third party packages
	_ "github.com/mattn/go-sqlite3"

	// Local packages
	"../models"
)

func InitDB(filepath string) *sql.DB {
	db, err := sql.Open("sqlite3", filepath)
	if err != nil { panic(err) }
	if db == nil { panic("db nil") }
	return db
}

func CreateTables(db *sql.DB) {

	var sql_table string
	var err error

	sql_table = `
	CREATE TABLE IF NOT EXISTS parkings(
		Id INTEGER NOT NULL PRIMARY KEY,
		Name TEXT,
		Long FLOAT,
		Lat FLOAT
	);
	`

	_, err = db.Exec(sql_table)
	if err != nil { panic(err) }

	sql_table = `
	CREATE TABLE IF NOT EXISTS sensors(
		Id INTEGER NOT NULL,
		Occupied BOOLEAN,
		Node INTEGER NOT NULL,
		Parking INTEGER NOT NULL,
		Faulty BOOLEAN,
		LastUpdateTime TEXT,
		PRIMARY KEY(Id, Node, Parking)
	);
	`

	_, err = db.Exec(sql_table)
	if err != nil { panic(err) }
}

func StoreSensor(db *sql.DB, sensors []models.Sensor) {
	sql_additem := `
	INSERT OR REPLACE INTO sensors(
		Id,
		Occupied,
		Node,
		Parking,
		Faulty,
		LastUpdateTime
	) values(?, ?, ?, ?, ?, ?)
	`

	stmt, err := db.Prepare(sql_additem)
	if err != nil { panic(err) }
	defer stmt.Close()

	for _, sensor := range sensors {
		_, err2 := stmt.Exec(
			sensor.Id,
			sensor.Occupied,
			sensor.Node,
			sensor.Parking,
			sensor.Faulty,
			time.Now().Format(time.RFC3339),
		)
		if err2 != nil { panic(err2) }
	}
}

func StoreParking(db *sql.DB, parkings []models.Parking) {
	sql_additem := `
	INSERT OR REPLACE INTO parkings(
		Id,
		Name,
		Long,
		Lat
	) values(?, ?, ?, ?)
	`

	stmt, err := db.Prepare(sql_additem)
	if err != nil { panic(err) }
	defer stmt.Close()

	for _, parking := range parkings {
		_, err2 := stmt.Exec(parking.Id, parking.Name, parking.Long, parking.Lat)
		if err2 != nil { panic(err2) }
	}
}

func ReadNodes(db *sql.DB) []models.Node {
	sql_readall := `
	SELECT * FROM nodes
	`

	rows, err := db.Query(sql_readall)
	if err != nil { panic(err) }
	defer rows.Close()

	var result []models.Node
	for rows.Next() {
		node := models.Node{}
		err2 := rows.Scan(&node.Id, &node.Parking)
		if err2 != nil { panic(err2) }
		result = append(result, node)
	}
	return result
}

func ReadSensors(db *sql.DB) []models.Sensor {
	sql_readall := `
	SELECT * FROM sensors
	ORDER BY Parking, Node, Id
	`
	rows, err := db.Query(sql_readall)
	if err != nil { panic(err) }
	defer rows.Close()

	var result []models.Sensor
	var timeString string
	for rows.Next() {
		sensor := models.Sensor{}
		err2 := rows.Scan(
			&sensor.Id,
			&sensor.Occupied,
			&sensor.Node,
			&sensor.Parking,
			&sensor.Faulty,
			&timeString,
		)
		if err2 != nil { panic(err2) }

		parsedTime, _  := time.Parse(time.RFC3339, timeString)
		sensor.SinceLastUpdate = time.Since(parsedTime).Seconds()

		result = append(result, sensor)
	}
	return result
}

func ReadSensorsFromParking(db *sql.DB, parking_id int) []models.Sensor {
	sql_readsensors := `
	SELECT * FROM sensors
	WHERE Parking=?
	ORDER BY Parking, Node, Id
	`

	stmt, err := db.Prepare(sql_readsensors)
	if err != nil { panic(err) }
	defer stmt.Close()

	rows, err2 := stmt.Query(parking_id)
	if err2 != nil { panic(err2) }
	defer rows.Close()

	var result []models.Sensor
	var timeString string
	for rows.Next() {
		sensor := models.Sensor{}
		err2 := rows.Scan(
			&sensor.Id,
			&sensor.Occupied,
			&sensor.Node,
			&sensor.Parking,
			&sensor.Faulty,
			&timeString,
		)
		if err2 != nil { panic(err2) }

		parsedTime, _  := time.Parse(time.RFC3339, timeString)
		sensor.SinceLastUpdate = time.Since(parsedTime).Seconds()

		result = append(result, sensor)
	}
	return result
}

func ReadParkings(db *sql.DB) []models.Parking {
	sql_readall := `
	SELECT * FROM parkings
	`
	rows, err := db.Query(sql_readall)
	if err != nil { panic(err) }
	defer rows.Close()

	var result []models.Parking
	for rows.Next() {
		parking := models.Parking{}
		err2 := rows.Scan(&parking.Id, &parking.Name, &parking.Long, &parking.Lat)
		if err2 != nil { panic(err2) }

		parking.Sensors = ReadSensorsFromParking(db, parking.Id)
		result = append(result, parking)
	}
	return result
}

func ReadParking(db *sql.DB, parking_id int) models.Parking {
	sql_readparking := `
	SELECT Id, Name, Long, Lat FROM parkings
	WHERE Id=?
	`

	stmt, err := db.Prepare(sql_readparking)
	if err != nil { panic(err) }
	defer stmt.Close()

	row, err2 := stmt.Query(parking_id)
	if err2 != nil { panic(err2) }
	defer row.Close()

	row.Next()
	parking := models.Parking{}
	err3 := row.Scan(&parking.Id, &parking.Name, &parking.Long, &parking.Lat)
	if err3 != nil { panic(err2) }

	parking.Sensors = ReadSensorsFromParking(db, parking_id)

	return parking
}
