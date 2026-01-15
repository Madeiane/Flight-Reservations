using System;
using System.IO;
using System.Diagnostics;

namespace Flight_Reservations
{
    public class TicketingService
    {
       public void GeneratePrintableTicket(Flight flight, ITicket ticket)
       {
    string fileName = $"BoardingPass_{ticket.PassengerId.Replace(" ", "_")}.html";

    string htmlContent = $@"
    <html>
    <head>
        <style>
            .ticket {{
                font-family: 'Helvetica Neue', Arial, sans-serif;
                width: 700px;
                height: 250px;
                background: white;
                border-radius: 15px;
                display: flex;
                box-shadow: 0 10px 30px rgba(0,0,0,0.1);
                overflow: hidden;
                border: 1px solid #ddd;
                margin: 50px auto;
            }}
            .main-section {{
                padding: 0;
                width: 70%;
                display: flex;
                flex-direction: column;
            }}
            .header {{
                background: #1a5f7a; /* Professional Blue */
                color: white;
                padding: 15px 25px;
                display: flex;
                justify-content: space-between;
                align-items: center;
            }}
            .content {{
                padding: 20px 25px;
                display: grid;
                grid-template-columns: 1fr 1fr 1fr;
                gap: 15px;
            }}
            .stub {{
                width: 30%;
                background: #f8f9fa;
                border-left: 2px dashed #ccc;
                padding: 20px;
                display: flex;
                flex-direction: column;
                justify-content: center;
            }}
            .label {{ color: #888; font-size: 10px; text-transform: uppercase; margin-bottom: 2px; }}
            .value {{ font-weight: bold; font-size: 14px; color: #333; }}
            .city-code {{ font-size: 24px; font-weight: 900; color: #1a5f7a; }}
            .barcode {{
                margin-top: auto;
                height: 40px;
                background: repeating-linear-gradient(90deg, #000, #000 2px, #fff 2px, #fff 4px);
                width: 100%;
            }}
        </style>
    </head>
    <body>
        <div class='ticket'>
            <div class='main-section'>
                <div class='header'>
                    <span style='font-size: 18px; font-weight: bold;'>UPT AIRWAYS</span>
                    <span style='font-size: 12px;'>{ticket.GetType().Name.Replace("Ticket", "").ToUpper()} CLASS</span>
                </div>
                <div class='content'>
                    <div>
                        <div class='label'>Passenger</div>
                        <div class='value'>{ticket.PassengerId}</div>
                    </div>
                    <div>
                        <div class='label'>Flight</div>
                        <div class='value'>{flight.FlightNumber}</div>
                    </div>
                    <div>
                        <div class='label'>Gate</div>
                        <div class='value'>B-12</div>
                    </div>
                    <div style='grid-column: span 3; display: flex; justify-content: space-between; align-items: center; margin-top: 10px;'>
                        <div>
                            <div class='city-code'>{flight.DepartureAirportId.ToString().ToUpper()}</div>
                            <div class='label'>Departure</div>
                        </div>
                        <div style='font-size: 20px; color: #ccc;'>✈</div>
                        <div style='text-align: right;'>
                            <div class='city-code'>{flight.ArrivalAirportId.ToString().ToUpper()}</div>
                            <div class='label'>Arrival</div>
                        </div>
                    </div>
                </div>
                <div class='barcode'></div>
            </div>
            <div class='stub'>
                <div class='label'>Boarding Pass</div>
                <div class='value' style='font-size: 18px; margin-bottom: 15px;'>{ticket.SeatNumber}</div>
                <div class='label'>Date</div>
                <div class='value'>{flight.DepartureTime:dd MMM yy}</div>
                <div class='label'>Time</div>
                <div class='value'>{flight.DepartureTime:HH:mm}</div>
                <div style='margin-top: auto; font-size: 9px; color: #aaa;'>ID: {ticket.TicketId.ToString().Substring(0,8)}</div>
            </div>
        </div>
    </body>
    </html>";

        File.WriteAllText(fileName, htmlContent);
        Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
    }
}