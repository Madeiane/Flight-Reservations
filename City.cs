﻿using System.Collections.Generic;

namespace Flight_Reservations
{
    public class City : Location
    {
        public string Country { get; set; }
        public List<Airport> Airports { get; set; } = new List<Airport>();

        public City(string name, string country) : base(name)
        {
            Country = country;
        }
        public void AddAirport(Airport airport)
        {
            Airports.Add(airport);
        }
    }
}