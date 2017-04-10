
var sensorProps = [
    'node',
    'id',
    'occupied',
    'faulty',
    'sinceLastUpdate'
]

var tableHeaders = [
    'Node',
    'Sensor',
    'Occupied',
    'Faulty',
    'Occupied time']

// Get lot ID from URL
function idFromURL() {
    var splitURL = location.href.split("/")
    var id = splitURL[splitURL.length-1]
    return(id) // Change this after debugging
}

function getSensorID(sensor) {
    return('sensor' + String(sensor.node) + String(sensor.id))
}

function timeString(sFloat) {
    var mins = Math.floor(sFloat/60.0)
    seconds = Math.floor(sFloat%60.0)

    var sString = String(seconds) + "s"
    var mString, hString
    if (mins == 0) {
        mString = ""
    } else {
        mString = String(mins) + "m"
    }
    return(mString + sString)
}

// Callback for the first request success
function firstSuccess(data) {

    document.getElementById('lotName').innerHTML = data.name

    var table = document.getElementById('lotData')

    var sensors = data.sensors
    sensors.forEach(function(sensor){

        var sensorRow = document.createElement('tr')
        sensorRow.id = getSensorID(sensor)

        sensorProps.forEach(function(property){
            var sensorCol = document.createElement('td')
            sensorCol.className = property
            sensorCol.innerHTML = String(sensor[property])
            sensorRow.appendChild(sensorCol)
        })
        table.appendChild(sensorRow)

        if (sensor.id == 4) {
            var rowSpace = document.createElement('tr')
            rowSpace.innerHTML = "<br>"
            table.appendChild(rowSpace)
        }
    })
}

function success(data) {

    var sensors = data.sensors
    sensors.forEach(function(sensor){

        var sensorRow = document.getElementById(getSensorID(sensor))

        sensorProps.forEach(function(property){
            var sensorCol = sensorRow.getElementsByClassName(String(property))

            switch(property) {
                case 'sinceLastUpdate':
                    sensorCol[0].innerHTML = timeString(sensor[property])
                    break
                case 'occupied':
                    if (sensor[property]) {
                        sensorCol[0].style.backgroundColor = '#dfb2b6'
                        sensorCol[0].innerHTML = "occupied"
                    } else {
                        sensorCol[0].style.backgroundColor = '#b6dfb2'
                        sensorCol[0].innerHTML = "free"
                    }
                    break
                case 'faulty':
                    if (sensor[property]) {
                        sensorRow.style.backgroundColor = '#fdd635'
                        var occupiedCell = sensorRow.getElementsByClassName('occupied')[0]
                        occupiedCell.style.backgroundColor = '#fdd635'
                        occupiedCell.innerHTML = "-"
                    }
                default:
                    sensorCol[0].innerHTML = String(sensor[property])
            }
        })
    })
}

// Recursive polling
function poll(successCallback) {
    var url =  "website/parking/" + idFromURL()

    $.ajax({
        dataType: "json",
        url: url,
        success: successCallback,
        complete: function () {
            setTimeout(function() {
                poll(success)
            }, 1000)
        },
        error: function () {
            console.log("error")
        }
    });
}

idFromURL()
poll(firstSuccess)
