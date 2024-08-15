using System.Text.Json;
using BajadorProbando.Models;
using BajadorProbando.Services;


var afipDates = Environment.GetEnvironmentVariable("AFIP_DATES");
var ipAddress = Environment.GetEnvironmentVariable("IP_ADDRESS");
var pemPath = Environment.GetEnvironmentVariable("PEM_PATH");

List<DayToInform> daySchedule = JsonSerializer.Deserialize<List<DayToInform>>(afipDates);

var Afip = new AfipService(daySchedule, ipAddress, pemPath);

Afip.Execute();
