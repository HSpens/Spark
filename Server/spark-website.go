package main

import (

    // Standard library packages
    "net/http"
    "html/template"
    "strconv"
    "flag"
    //"encoding/json"

    // Third party packages
    "github.com/julienschmidt/httprouter"
)

func MainGetHandle(w http.ResponseWriter, r *http.Request, _ httprouter.Params) {
    t,_ := template.ParseFiles("static/map.html")
    t.Execute(w, nil)
}

func LotGetHandle(w http.ResponseWriter, r *http.Request, ps httprouter.Params) {
    t,_ := template.ParseFiles("static/lot.html")
    t.Execute(w, nil)
}

func main() {

    // Instantiate a new router
    router := httprouter.New()

    // GET handler
    router.GET("/", MainGetHandle)
    router.GET("/parking/:id", LotGetHandle)
    router.ServeFiles("/static/*filepath", http.Dir("static"))

    // Parse flags
    var host = flag.String("host", "localhost", "Domain name of the place to host the service.")
    var port = flag.Int("port", 8080, "Port to serve http on.")
    flag.Parse()

    // Fire up the server
    http.ListenAndServe(*host + ":" + strconv.Itoa(*port), router)
}
