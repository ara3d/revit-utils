﻿using System;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace Revit.SDK.Samples.ProjectInfo.CS
{
    /// <summary>
    /// Wrapper class for SiteLocation
    /// </summary>
    public class SiteLocationWrapper : IWrapper
    {
        #region Fields
        /// <summary>
        /// SiteLocation
        /// </summary>
        private SiteLocation m_siteLocation;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes private variables.
        /// </summary>
        /// <param name="siteLocation"></param>
        public SiteLocationWrapper(SiteLocation siteLocation)
        {
            m_siteLocation = siteLocation;
            //m_citys = RevitStartInfo.RevitApp.Cities;
        } 
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets TimeZone
        /// </summary>
        [DisplayName("Time Zone"), TypeConverter(typeof(TimeZoneConverter))]
        public String TimeZone => GetTimeZoneFromDouble(m_siteLocation.TimeZone);

        //set
        //{
        //    m_siteLocation.TimeZone = GetTimeZoneFromString(value);
        //}
        /// <summary>
        /// Gets or sets Longitude
        /// </summary>
        [DisplayName("Longitude"), TypeConverter(typeof(AngleConverter))]
        public double Longitude
        {
            get => m_siteLocation.Longitude;
            set => m_siteLocation.Longitude = value;
        }

        /// <summary>
        /// Gets or sets Latitude
        /// </summary>
        [DisplayName("Latitude"), TypeConverter(typeof(AngleConverter))]
        public double Latitude
        {
            get => m_siteLocation.Latitude;
            set => m_siteLocation.Latitude = value;
        }

        [DisplayName("City"), TypeConverter(typeof(CityConverter))]
        public City City
        {
            get => GetCityFromPosition(Latitude, Longitude);
            set
            {
                m_siteLocation.Latitude = value.Latitude;
                m_siteLocation.Longitude = value.Longitude;
                m_siteLocation.TimeZone = value.TimeZone;
            }
        }

        private City GetCityFromPosition(double latitude, double longitude)
        {
            foreach (City city in RevitStartInfo.RevitApp.Cities)
            {
                if (DoubleEquals(city.Latitude, latitude) && DoubleEquals(city.Longitude, longitude))
                    return city;
            }
            return null;
        }

        public static bool DoubleEquals(double x, double y)
        {
            return Math.Abs(x - y) < 1E-9;
        }

        #region IWrapper Members

        /// <summary>
        /// Gets the handle object.
        /// </summary>
        [Browsable(false)]
        public object Handle => m_siteLocation;

        /// <summary>
        /// Gets the name of the handle.
        /// </summary>
        [Browsable(false)]
        public String Name
        {
            get => m_siteLocation.Name;
            set => m_siteLocation.Name = value;
        }
        #endregion 

        #endregion

        #region Methods

        /// <summary>
        /// Get time zone double value from time zone string
        /// </summary>
        /// <param name="value">time zone string</param>
        /// <returns>the value of time zone</returns>
        private double GetTimeZoneFromString(string value)
        {
            //i.e. convert "(GMT-12:00) International Date Line West" to 12.0
            //i.e. convert "(GMT-03:30) Newfoundland" to 3.30
            var timeZoneDouble = value.Substring(4, value.IndexOf(')') - 4).Replace(':', '.').Trim();
            if (string.IsNullOrEmpty(timeZoneDouble))
                return 0d;
            else
                return Double.Parse(timeZoneDouble);
        }

        /// <summary>
        /// Get time zone display string from time zone value
        /// </summary>
        /// <param name="timeZone">zone value</param>
        /// <returns>display string</returns>
        private string GetTimeZoneFromDouble(double timeZone)
        {
            // e.g. get "(GMT-04:00) Santiago" from double number 4.0
            // should find the last one who matches the time zone
            string lastTimeZone = null;
            foreach (var tmpTimeZone in RevitStartInfo.TimeZones)
            {
                object tmpZone = GetTimeZoneFromString(tmpTimeZone);
                if ((double)tmpZone == timeZone)
                    lastTimeZone = tmpTimeZone;
            }
            return lastTimeZone;
        }
        #endregion
    }
}