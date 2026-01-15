﻿using System;

namespace Flight_Reservations;

public class Copilot : Person
{
    public bool CertificatAvansat { get; }

    public Copilot(string nume, int varsta, bool certificatAvansat)
        : base(nume, varsta)
    {
        if (!certificatAvansat)
            throw new ArgumentException("Copilot must have advanced certification.");

        CertificatAvansat = certificatAvansat;
    }
}