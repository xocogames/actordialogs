using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextActors
{
    static public string TS_FORMAT_DATE = "yyyy/MM/dd";
    static public string TS_FORMAT = "yyyy/MM/dd HH:mm:ss";

    private DateTime dateTime;
    private DateTime dateTimeStartDay;

    private static float[] gameDayHourRel = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

    private int timeDawnStartHour = 6;
    private int timeDawnStartMin = 0;
    private int timeDawnDurationMin = 60;

    private DateTime tsDawnStart;
    private DateTime tsDawnMid;
    private DateTime tsDawnEnd;

    private int timeTwilightStartHour = 21;
    private int timeTwilightStartMin = 0;
    private int timeTwilightDurationMin = 60;

    private DateTime tsTwilightStart;
    private DateTime tsTwilightMid;
    private DateTime tsTwilightEnd;

    public ContextActors()
    {
        dateTime = new DateTime();
        dateTimeStartDay = GetStartDay(dateTime);

        SetDawnAndTwilightTimes();
    }

    /// <summary>
    /// Establece la relación de tiempo real respecto al tiempo de juego. <br>
    /// Se debe establecer un array de 24 horas, indicando en cada elemento del array, cuantos minutos tiene cada hora. <br>
    /// Por ejemplo, por defecto se establece que cada hora de juego durará 5 minutos en la vida real, estableciendo el array:  5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
    /// </summary>
    /// <param name="dayHourRel">Array de 24 floats que representan las 24 hora del día, indicando en cada elemento del array cuantos minutos tiene cada hora</param>
    public void SetDayHoursRelationTime(float[] dayHourRel)
    {
        if (dayHourRel.Length == 24)
        {
            gameDayHourRel = dayHourRel;
        } else
        {
            string str = "";
            foreach (float num in gameDayHourRel) str = (str.Length > 0 ? str + ", " + num : str + num) ;
            throw new Exception("DayHoursRelationTime error. Es necesario establecer un array de 24 horas con el número de minutos que tendrá cada hora. Por defecto: "+ str);
        }
    }

    public void SetStartDateTime(string ts)
    {
        SetStartDateTime(ts, TS_FORMAT);
    }

    public void SetStartDateTime(string ts, string format)
    {
        dateTime = DateTime.ParseExact(ts, format, null);
        dateTimeStartDay = GetStartDay(dateTime);
    }

    public void UpdatetDateTime(string ts)
    {
        UpdatetDateTime(ts, TS_FORMAT);
    }

    public void UpdatetDateTime(string ts, string format)
    {
        dateTime = DateTime.ParseExact(ts, format, null);
    }

    public void SetTimeAutoDawn(int hourStart, int minStart, int minDuration)
    {
        timeDawnStartHour = hourStart;
        timeDawnStartMin = minStart;
        timeDawnDurationMin = minDuration;

        SetDawnAndTwilightTimes();
    }

    public void SetTimeAutoTwilight(int hourStart, int minStart, int minDuration)
    {
        timeTwilightStartHour = hourStart;
        timeTwilightStartMin = minStart;
        timeTwilightDurationMin = minDuration;

        SetDawnAndTwilightTimes();
    }

    public void AddDeltaTimeReal(float delta)
    {
        float minutsHour = gameDayHourRel[dateTime.Hour];
        float timeRel = 60 / minutsHour;

        dateTime = dateTime.AddSeconds(delta * timeRel);
    }

    public int GetTimeGameHour()
    {
        Debug.Log("GAMETIME.GetTimeGameHour(). dateTime = "+ dateTime.ToString("yyyy/MM/dd HH:mm:ss"));
        return dateTime.Hour;
    }

    // Deveulve el numero de dias que llevas de juego, empezando a contar desde el primer instante del amanecer del primer día de juego.
    public int GetTimeGameDays()
    {
        int days = (dateTime - dateTimeStartDay).Days;
        Debug.Log("GAMETIME.GetTimeGameDays(). dateTime("+ dateTime.ToString("yyyy/MM/dd HH:mm:ss") + ") - dateTimeStartDay("+ dateTime.ToString("yyyy/MM/dd HH:mm:ss") + ") = " + days);
        return days;
    }

    public string GetTimeDate()
    {
        string date = dateTime.ToString(TS_FORMAT_DATE);
        Debug.Log("GAMETIME.GetTimeDate(). dateTime(" + dateTime.ToString("yyyy/MM/dd HH:mm:ss") + ") / date = (" + date + ")");
        return date;
    }

    private void SetDawnAndTwilightTimes()
    {
        tsDawnStart = new DateTime(1970, 1, 1, timeDawnStartHour, timeDawnStartMin, 0, 0);
        tsDawnMid = tsDawnStart.AddMinutes(timeDawnDurationMin /2);
        tsDawnEnd = tsDawnStart.AddMinutes(timeDawnDurationMin);

        tsTwilightStart = new DateTime(1970, 1, 1, timeTwilightStartHour, timeTwilightStartMin, 0, 0);
        tsTwilightMid = tsTwilightStart.AddMinutes(timeTwilightDurationMin /2);
        tsTwilightEnd = tsTwilightStart.AddMinutes(timeTwilightDurationMin);

        dateTimeStartDay = GetStartDay(dateTime);
    }

    private DateTime GetTimeWithoutDate(DateTime ts)
    {
        return new DateTime(1970, 1, 1, ts.Hour, ts.Minute, ts.Second, ts.Millisecond);
    }

    public DateTime GetStartDay(DateTime anyDateTime)
    {
        return new DateTime(anyDateTime.Year, anyDateTime.Month, anyDateTime.Day, timeDawnStartHour, timeDawnStartMin, 0);
    }

    public bool IsDateTimeInRange(DateTime startDate, DateTime dateToCheck, DateTime endDate)
    {
        return dateToCheck >= startDate && dateToCheck < endDate;
    }

    public bool IsTimeDay()
    {
        DateTime dateToCheck = GetTimeWithoutDate(dateTime);
        return IsDateTimeInRange(tsDawnMid, dateToCheck, tsTwilightMid);
    }

    public bool IsTimeNight()
    {
        return !IsTimeDay();
    }

    public bool IsTimeDawn()
    {
        DateTime dateToCheck = GetTimeWithoutDate(dateTime);
        return IsDateTimeInRange(tsDawnStart, dateToCheck, tsDawnEnd);
    }

    public bool IsTimeTwilight()
    {
        DateTime dateToCheck = GetTimeWithoutDate(dateTime);
        return IsDateTimeInRange(tsTwilightStart, dateToCheck, tsTwilightEnd);
    }

    public string ToStr(int num)
    {
        return num.ToString();
    }
}
