package main

import (

    // Standard library packages
    "fmt"
    "net/http"
    "encoding/json"
    "strconv"
    "flag"

    // Third party packages
    "github.com/julienschmidt/httprouter"

    // Local packages
    "./db"
    "./models"
)

const dbpath = "database.db"

// BasicAuth
func BasicAuth(h httprouter.Handle, requiredUser, requiredPassword string) httprouter.Handle {
    return func(w http.ResponseWriter, r *http.Request, ps httprouter.Params) {

        // Get the Basic Authentication credentials
        user, password, hasAuth := r.BasicAuth()

        if hasAuth && user == requiredUser && password == requiredPassword {
            // Delegate request to the given handle
            h(w, r, ps)
        } else {
            // Request Basic Authentication otherwise
            w.Header().Set("WWW-Authenticate", "Basic realm=Restricted")
            http.Error(w, http.StatusText(http.StatusUnauthorized), http.StatusUnauthorized)
        }
    }
}

func ParkingPostHandle(w http.ResponseWriter, r *http.Request, _ httprouter.Params) {

    // Initialize list of nodes
    parkings := []models.Parking{}

    // Populate from POST data
    err := json.NewDecoder(r.Body).Decode(&parkings)
    if err != nil {
        fmt.Println(err)
    }
    defer r.Body.Close()

    // Store data
    database := db.InitDB(dbpath)
    defer database.Close()
    db.StoreParking(database, parkings)

    // Print
    parkingsj, _ := json.Marshal(parkings)

    fmt.Printf("%s", parkingsj)

    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(201)
    fmt.Fprintf(w, "%s\n", parkingsj)
}

func SensorPostHandle(w http.ResponseWriter, r *http.Request, _ httprouter.Params) {

    // Initialize list of nodes
    sensors := []models.Sensor{}

    // Populate from POST data
    err := json.NewDecoder(r.Body).Decode(&sensors)
    if err != nil {
        fmt.Println("Error")
    }
    defer r.Body.Close()

    fmt.Println(sensors)

    // Store data
    database := db.InitDB(dbpath)
    defer database.Close()
    db.StoreSensor(database, sensors)

    // Print
    sensorsj, _ := json.Marshal(sensors)

    fmt.Printf("%s", sensorsj)

    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(201)
    fmt.Fprintf(w, "%s\n", sensorsj)
}

func ParkingsGetHandle(w http.ResponseWriter, r *http.Request, ps httprouter.Params) {
    database := db.InitDB(dbpath)
    defer database.Close()

    parkings := db.ReadParkings(database)

    parkingsj, _ := json.MarshalIndent(parkings, "", "   ")

    w.Header().Set("Content-Type", "application/json")
    w.Header().Set("Access-Control-Allow-Origin", "*")
    w.WriteHeader(200)
    fmt.Fprintf(w, "%s\n", parkingsj)
}

func ParkingGetHandle(w http.ResponseWriter, r *http.Request, ps httprouter.Params) {
    database := db.InitDB(dbpath)
    defer database.Close()

    parking_id, _ := strconv.Atoi(ps.ByName("id"))
    parking := db.ReadParking(database, parking_id)

    parkingj, _ := json.Marshal(parking)

    w.Header().Set("Content-Type", "application/json")
    w.Header().Set("Access-Control-Allow-Origin", "*")
    w.WriteHeader(200)
    fmt.Fprintf(w, "%s\n", parkingj)
}

func main() {

    user := "User"
    pass := "Password"

    // Create database tables if there aren't any
    const dbpath = "database.db"
	database := db.InitDB(dbpath)
	defer database.Close()
	db.CreateTables(database)

    // Instantiate a new router
    router := httprouter.New()

    // GET handlers
    router.GET("/parking", ParkingsGetHandle)
    router.GET("/parking/:id", ParkingGetHandle)

    // POST handlers
    router.POST("/parking", BasicAuth(ParkingPostHandle, user, pass))
    router.POST("/sensor", BasicAuth(SensorPostHandle, user, pass))

    // Parse flags
    var host = flag.String("host", "localhost", "Domain name of the place to host the service.")
    var port = flag.Int("port", 8000, "Port to serve http on.")
    flag.Parse()

    // Fire up the server
    http.ListenAndServe(*host + ":" + strconv.Itoa(*port), router)
}
