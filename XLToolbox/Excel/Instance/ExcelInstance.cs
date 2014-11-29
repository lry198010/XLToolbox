﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Excel;

namespace XLToolbox.Excel.Instance
{
    /// <summary>
    /// Provide access to an instance of Excel that the
    /// components are to work with.
    /// </summary>
    /// <remarks>
    /// <para>This class uses static fields to make sure only one
    /// instance of Excel is invoked. An internal counter records
    /// the number of class instances that are currently in use;
    /// when the last instance of this class is disposed of, the
    /// Excel instance will be closed.</para>
    /// <para>Note that this class will only reference one single
    /// Excel instance, regardless whether this was started using
    /// a static method or by instantiating the class. Thus, there
    /// is no instance property to access the Exce instance, just
    /// the static property. Instantiating this class mainly serves
    /// the purpose of being able to automatically close Excel when
    /// the work is done by using Using() structures.</para>
    /// </remarks>
    public class ExcelInstance : IDisposable
    {
        #region Private members

        private bool _disposed;
        private static bool _static;
        private static int _numClassInstances;

        /// <summary>
        /// Holds the current Excel instance; static field as only one instance
        /// is allowed to be connected with the XL Toolbox at a time.
        /// </summary>
        private static Application _application;

        #endregion

        #region Public properties

        /// <summary>
        /// Provides access to the current Excel instance.
        /// </summary>
        public static Application Application
        {
            get
            {
                if (_application == null)
                {
                    throw new ExcelInstanceException("No instance running.");
                }
                return _application;
            }
            set
            {
                if (_application != null)
                {
                    throw new ExcelInstanceAlreadySetException();
                }
                _application = value;
            }
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Starts a new instance of Excel. Does nothing if there already is an instance.
        /// </summary>
        public static void Start()
        {
            Start(true);
        }

        /// <summary>
        /// Shuts down the current instance of Excel; no warning message will be shown.
        /// If an instance of this class exists, an error will be thrown.
        /// </summary>
        public static void Shutdown()
        {
            if (_numClassInstances != 0)
            {
                throw new ExcelInstanceException(String.Format(
                    "There are still {0} class instances.",
                    _numClassInstances));
            }
            Application.DisplayAlerts = false;
            Application.Quit();
            _static = false;
            _application = null;
        }

        /// <summary>
        /// Creates and returns a new workbook containing exactly one worksheet.
        /// </summary>
        /// <returns>Workbook with only one worksheet.</returns>
        public static Workbook CreateWorkbook()
        {
            // Calling the Workbooks.Add method with a XlWBATemplate constand
            // creates a workbook that contains only one sheet.
            return ExcelInstance.Application.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);
        }

        /// <summary>
        /// Creates a workbook containing the specified number of sheets (not less than 1).
        /// </summary>
        /// <remarks>If <paramref name="numberOfSheets"/> is less than 1, the workbook will still
        /// contain one worksheet.</remarks>
        /// <param name="numberOfSheets">Number of sheets in the new workbook.</param>
        /// <returns>Workbook containing the specified number of sheets (not less than 1).</returns>
        public static Workbook CreateWorkbook(int numberOfSheets)
        {
            Workbook wb = CreateWorkbook();
            for (int i = 2; i <= numberOfSheets; i++)
            {
                wb.Sheets.Add();
            };
            return wb;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiates this class. Invokes a new Excel instance if none is running.
        /// </summary>
        /// <remarks>The number of class instances is recorded in the private field
        /// <see cref="_numClassInstances"/>.</remarks>
        public ExcelInstance()
        {
            _numClassInstances += 1;
            Start(false);
        }

        #endregion

        #region Disposing

        ~ExcelInstance()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _numClassInstances -= 1;
                // Only shut down Excel if there are no other instances of
                // this class *and* if Excel has not been invoked by the
                // static methods of this class.
                if (_numClassInstances == 0 && !_static)
                {
                    Shutdown();
                }
            }
        }

        #endregion

        #region Private methods

        private static void Start(bool isStatic)
        {
            _static |= isStatic;
            if (_application == null)
            {
                Application = new Application();
                Application.Workbooks.Add();
            }
        }

        #endregion
    }
}