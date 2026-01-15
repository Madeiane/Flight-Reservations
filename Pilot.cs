﻿using System;

namespace Flight_Reservations;

public class Pilot : Person
{
    public int OreZbor { get; }

    public Pilot(string nume, int varsta, int oreZbor)
        : base(nume, varsta)
    {
        if (oreZbor < 1000)
            throw new ArgumentException("Pilot must have at least 1000 flight hours.");

        OreZbor = oreZbor;
    }
}