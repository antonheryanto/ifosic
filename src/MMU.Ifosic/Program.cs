using MMU.Ifosic;

var fdd = FrequencyShiftDistance.Load(@"C:\Projects\MMU\Set01.bin");
Console.WriteLine(fdd.CalibrationTemperature.Count);