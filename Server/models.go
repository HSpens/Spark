package models

type Parking struct {
	Id int `json:"id"`
    Name string `json:"name"`
	Long float32 `json:"lng"`
	Lat float32 `json:"lat"`
	Sensors []Sensor `json:"sensors"`
}

// Currently not used
type Node struct {
	Id int `json:"id"`
	Parking int `json:"parking"`
}

type Sensor struct {
	Id int `json:"id"`
	Occupied bool `json:"occupied"`
	Node int `json:"node"`
	Parking int `json:"parking"`
	Faulty bool `json:"faulty"`
	SinceLastUpdate float64 `json:"sinceLastUpdate"`
}
