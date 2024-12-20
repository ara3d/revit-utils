﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RevitElementBipChecker.Model
{
    public static class ExcelUtils
    {
        /// <summary>
        /// Save Report To File Csv
        /// </summary>
        /// <param name="dt">data table</param>
        /// <param name="path">output path of file</param>
        /// <param name="filename">filename want save</param>
        public static void OpenExcel(this DataTable dt, out string path, string filename = "Report.csv")
        {
            var PathDocument = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string namesave;
            if (filename.ToLower().Contains(".csv"))
            {
                namesave = filename;
            }
            else
            {
                namesave = filename + ".csv";
            }
            path = Path.Combine(PathDocument, namesave);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var lines = new List<string>();

            var columnNames = dt.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToArray();

            var header = string.Join(",", columnNames.Select(name => $"\"{name}\""));
            lines.Add(header);

            var valueLines = dt.AsEnumerable()
                .Select(row => string.Join(",", row.ItemArray.Select(val => $"\"{val.ToString().FixUnitInch()}\"")));

            lines.AddRange(valueLines);

            File.WriteAllLines(path, lines,Encoding.UTF8);
        }

        /// <summary>
        /// convert Data Object to data table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            var props =
                TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            for(var i = 0 ; i < props.Count ; i++)
            {
                var prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            var values = new object[props.Count];
            foreach (var item in data)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;        
        }
       
        /// <summary>
        /// convert optimize data object to data table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable ToDataTable2<T>(this IList<T> data)
        {
            var properties = 
                TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (var item in data)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }


        /// <summary>
        /// Decode Unicode 8
        /// </summary>
        /// <param name="utf8String"></param>
        /// <returns></returns>
        public static string DecodeFromUtf8(this string utf8String)
        {
            // copy the string as UTF-8 bytes.
            var utf8Bytes = new byte[utf8String.Length];
            for (var i=0;i<utf8String.Length;++i) {
                //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
                utf8Bytes[i] = (byte)utf8String[i];
            }

            return Encoding.UTF8.GetString(utf8Bytes,0,utf8Bytes.Length);
        }

        public static string FixUnitInch(this string str)
        {
            var pattern = "\"$";
            var regex = new Regex(pattern);
            if (regex.IsMatch(str))
            { 
                return str.Replace(str, str + " ");
            }
            return str;
        }
    }
}
