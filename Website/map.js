/* --- OSRM ---*/
var osrmUrl = 'https://router.project-osrm.org/'

// OSRM request functions
var osrm = {

    // Get closest coordinate in the street network
    getNearest: function(coord) {
        var url = osrmUrl + 'nearest/v1/driving/' + coord.join()
        return($.getJSON(url))
    },

    // Get the closest destination from a source location
    getClosestDestination: function(source, destinations, callback) {
        var url = osrmUrl + 'table/v1/driving/' + source.join()
        for (var i=0; i<destinations.length; i++) {
            url += ';' + destinations[i].join()
        }
        url += '?sources=0'
        $.getJSON(url, callback)
    },

    // Get route between two locations
    getRoute: function(source, destination, callback) {

        // Snap coordinates to nearst street coordinates
        $.when(this.getNearest(source), this.getNearest(destination)).done(function(rSource, rDest) {
            var snapSource = rSource[0].waypoints[0].location
            var snapDestination = rDest[0].waypoints[0].location

            // Get route
            var url = osrmUrl + 'route/v1/driving/'
            url += snapSource.join() + ';' + snapDestination.join()
            $.getJSON(url, callback)
        })
    }
}

/* --- OpenLayers ---*/

// Adds a route to the closest destination from source
// Note that destinations needs to be an array of arrays
function addShortestRoute(source, destinations) {

    // Get closest destination and use callback on the result
    osrm.getClosestDestination(source, destinations, function(data) {

        // Use slice to remove source form result
        var durations = data.durations[0].slice(1)
        var destinations = data.destinations.slice(1)

        // Minimize duration
        var ind = durations.indexOf(Math.min(...durations))
        var destination = destinations[ind].location

        osrm.getRoute(source, destination, function(data) {
            console.log(data)

            var route = data.routes[0].geometry
            var routePolyline = new ol.format.Polyline().readGeometry(route, {
                dataProjection: 'EPSG:4326',
                featureProjection: 'EPSG:3857'
            })
            var routeFeature = new ol.Feature({
                type: 'route',
                geometry: routePolyline
            });
            routeFeature.setStyle(new ol.style.Style({
                stroke: new ol.style.Stroke({
                    width: 6, color: [40, 40, 40, 0.8]
                })
            }))
            routeVectorSource.clear()
            routeVectorSource.addFeature(routeFeature)

            panZoomToGeometry(routePolyline)
        })
    })
}

// Moves the view to a location with a pan animation
function panToLocation(coordinates) {
    var pan = ol.animation.pan({
        duration: 2000,
        source: view.getCenter()
    })
    map.beforeRender(pan)
    view.setCenter(coordinates)
}

// Pans to fit a route
function panZoomToGeometry(geometry) {
    var pan = ol.animation.pan({
        duration: 2000,
        source: view.getCenter(),
        easing: ol.easing.easeOut
    })
    var zoom = ol.animation.zoom({
        resolution: map.getView().getResolution(),
        duration: 2000,
        easing: ol.easing.easeOut
    })
    map.beforeRender(pan, zoom)
    view.fit(geometry, map.getSize(), {
        padding: [40, 40, 40, 40]
    })
}

// Adds parking markers to map
var parkingPoints = new ol.geom.MultiPoint()
function addParkingMarkers() {
    var url = 'website/parking'
    $.getJSON(url, function(parkings) {
        parkings.forEach(function(parking, index) {
            var coord = [parking.lng, parking.lat]
            var point = new ol.geom.Point(ol.proj.fromLonLat(coord))
            var iconFeature = new ol.Feature({
                geometry: point,
                type: 'parking',
                data: parking
            })
            var iconStyle = new ol.style.Style({
                image: new ol.style.Icon({
                    src: 'https://cdn2.iconfinder.com/data/icons/snipicons/500/map-marker-128.png',
                    scale: 0.4,
                    anchor: [0.5, 1]
                })
            })
            iconFeature.setStyle(iconStyle)
            positionVectorSource.addFeature(iconFeature)
            parkingPoints.appendPoint(point)
        })
        panZoomToGeometry(parkingPoints)
    })
}

// Returns an HTML representation of a parking
function parkingToPopupHTML(parking) {
    var sensors = parking.sensors
    var tot = sensors.length
    var nAvailable = 0
    sensors.forEach(function(sensor){
        if (!sensor.occupied) {
            nAvailable++
        }
    })

    var html = ' \
        <style> \
            td { \
                padding-right: 5px; \
                padding-left: 5px; \
                white-space: nowrap; \
            } \
        </style> \
        <div style="text-align: center;"> \
        <table> \
            <tr> \
                <td>Available parkings:</td> \
                <td>'+ nAvailable + "/" + tot +'</td> \
            </tr> \
        </table> \
        <div id="loDetailsButton" type="button" class="btn btn-default" onmouseup="lotDetailsCallback('+ parking.id +')">Lot details</div> \
        <div id="getRouteButton" type="button" class="routeButton btn btn-default" onmouseup="getRouteCallback('+ JSON.stringify([parking.lng, parking.lat]) +')">Get route</div> \
        </div> \
    '

    return(html)
}

/* Callbacks */

function overviewCallback() {
    panZoomToGeometry(parkingPoints)
}

function closestLotCallback() {
    var coord = geolocation.getPosition();

    parkingCoordinates = parkingPoints.getCoordinates()
    parkingCoordinates.forEach(function(coordinate, i) {
            parkingCoordinates[i] = ol.proj.toLonLat(coordinate)
    })
    addShortestRoute(ol.proj.toLonLat(coord), parkingCoordinates)
}

function lotDetailsCallback(parkingId) {
    var newPathname = "/parking/" + parkingId
    console.log(newPathname);
    location.pathname = newPathname
}

function getRouteCallback(parkingCoord) {
    console.log(parkingCoord)
    var coord = geolocation.getPosition();
    addShortestRoute(ol.proj.toLonLat(coord), [parkingCoord])
}

// View
var view = new ol.View({
    center: ol.proj.fromLonLat([18.071556, 59.326565]),
    zoom: 3,
    minZoom: 3
})

// Map
var map = new ol.Map({
  target: 'map',
  layers: [
    new ol.layer.Tile({
      source: new ol.source.OSM()
    })
  ],
  view: view,
  loadTilesWhileAnimating: true
});

// Parking popup
var parkingPopup = new ol.Overlay({
    element: document.getElementById('parkingPopup')
})
map.addOverlay(parkingPopup)

function createPopupDiv() {
    var div = document.createElement('div')
    document.body.appendChild(div)
    return div
}

map.on('click', function(evt) {

    var element = createPopupDiv()

    parkingPopup.setElement(element)

    var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature) {
        return feature
    })

    if (feature && feature.get('type') == 'parking') {
        console.log(feature.get('data'))
        var coord = feature.getGeometry().getCoordinates()
        var content = parkingToPopupHTML(feature.get('data'))
        console.log(feature.get('data').name)

        parkingPopup.setPosition(coord)
        $(element).popover({
            'placement': 'top',
            'html': true,
            'content': content,
            'title': feature.get('data').name
        });

        // Ugly hack to address problem with the mobile click problem
        setTimeout(function() {
            $(element).popover('show');
        }, 500)

        // Ugly hack since it doesn't seem to work to alter the HTML in 'content'
        if (!geolocation.getPosition()) {
            $(".routeButton").attr('disabled', true)
        } else {
            $(".routeButton").attr('disabled', false)
        }
    } else {
        $(element).popover('destroy');
    }
})

// Geolocation
var geolocation = new ol.Geolocation({
    tracking: true,
    projection: view.getProjection()
});

geolocation.on('error', function(error) {
    console.log("error")
});

// Position feature
var positionFeature = new ol.Feature();
positionFeature.setStyle(new ol.style.Style({
    image: new ol.style.Circle({
        radius: 6,
        fill: new ol.style.Fill({
            color: '#3399CC'
        }),
        stroke: new ol.style.Stroke({
            color: '#fff',
            width: 2
        })
    })
}));

// Show accuracy around positon feature
var accuracyFeature = new ol.Feature();
geolocation.on('change:accuracyGeometry', function() {
    accuracyFeature.setGeometry(geolocation.getAccuracyGeometry());
});

// Move position feature when position changes
geolocation.on('change:position', function() {
    var coordinates = geolocation.getPosition();
    positionFeature.setGeometry(coordinates ?
        new ol.geom.Point(coordinates) : null);
    console.log("Position changed");
});

// Pan to location once in the beginning
// This will happen once the user accepts the use of geolocation
geolocation.once('change:position', function() {
    //$("#closestParkingButton").attr('disabled', false)
    $(".routeButton").attr('disabled', false)
    var coordinates = geolocation.getPosition();
    console.log(coordinates)
});

// Add layer with position and accuracy features
var positionVectorSource = new ol.source.Vector()
var vectorLayer = new ol.layer.Vector({
    map: map,
    source: positionVectorSource
});
positionVectorSource.addFeatures([accuracyFeature, positionFeature])

// Add layer for routes
var routeVectorSource = new ol.source.Vector()
var vectorLayer = new ol.layer.Vector({
    map: map,
    source: routeVectorSource
});

addParkingMarkers()
