﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Microsoft.Office.Interop.Excel;
using XLToolbox.Export;
using XLToolbox.Excel.ViewModels;
using XLToolbox.Excel.Instance;
using Bovender.Unmanaged;

namespace XLToolbox.UnitTests.Export
{
    [TestFixture]
    class ExporterTest
    {
        [Test]
        [RequiresSTA]
        public void ExportChartObject()
        {
            using (ExcelInstance excel = new ExcelInstance())
            {
                Workbook wb = ExcelInstance.CreateWorkbook();
                Worksheet ws = wb.Worksheets[1];
                ws.Cells[1, 1] = 1;
                ws.Cells[2, 1] = 2;
                ws.Cells[3, 1] = 3;
                ChartObjects cos = ws.ChartObjects();
                ChartObject co = cos.Add(20, 20, 300, 200);
                SeriesCollection sc = co.Chart.SeriesCollection();
                sc.Add(ws.Range["A1:A3"]);
                co.Chart.ChartArea.Select();
                Preset preset = new Preset(FileType.Png, 300, ColorSpace.Rgb);
                SingleExportSettings settings = new SingleExportSettings(preset,
                    co.Width, co.Height, true);
                settings.FileName = 
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "XL Toolbox Export Test.png"
                    );
                Exporter exporter = new Exporter();
                exporter.ExportSelection(settings);
            }
        }
    }
}
