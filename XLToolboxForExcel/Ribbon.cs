﻿/* Ribbon.cs
 * part of Daniel's XL Toolbox NG
 * 
 * Copyright 2014-2015 Daniel Kraus
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Resources;
using Office = Microsoft.Office.Core;
using CustomUI = DocumentFormat.OpenXml.Office.CustomUI;
using Xl = Microsoft.Office.Interop.Excel;

// TODO:  Follow these steps to enable the Ribbon (XML) item:

// 1: Copy the following code block into the ThisAddin, ThisWorkbook, or ThisDocument class.

//  protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
//  {
//      return new Ribbon1();
//  }

// 2. Create callback methods in the "Ribbon Callbacks" region of this class to handle user
//    actions, such as clicking a button. Note: if you have exported this Ribbon from the Ribbon designer,
//    move your code from the event handlers to the callback methods and modify the code to work with the
//    Ribbon extensibility (RibbonX) programming model.

// 3. Assign attributes to the control tags in the Ribbon XML file to identify the appropriate callback methods in your code.  

// For more information, see the Ribbon XML documentation in the Visual Studio Tools for Office Help.

namespace XLToolbox
{
    [ComVisible(true)]
    public class Ribbon : Office.IRibbonExtensibility
    {
        #region Constructor (add command dictionary entries here)

        public Ribbon()
        {
            _commandDictionary = new Dictionary<string, Command>()
            {
                { "ButtonAbout", Command.About },
                { "ButtonCheckForUpdate", Command.CheckForUpdates },
                { "ButtonTestError", Command.ThrowError },
                { "ButtonSheetList", Command.SheetManager },
                { "ButtonExportSelection", Command.ExportSelection },
                { "ButtonExportSelectionQuick", Command.ExportSelectionLast },
                { "ButtonExportBatch", Command.BatchExport },
                { "ButtonExportBatchQuick", Command.BatchExportLast },
                { "ButtonExportScreenshot", Command.ExportScreenshot },
            };

            Versioning.UpdaterViewModel.Instance.PropertyChanged += UpdaterViewModel_PropertyChanged;
        }

        #endregion

        #region IRibbonExtensibility Members

        public string GetCustomUI(string ribbonID)
        {
            return GetResourceText("XLToolbox.Ribbon.xml");
        }

        #endregion

        #region Ribbon Callbacks

        public void Button_OnAction(Office.IRibbonControl control)
        {
            Command cmd;
            if (_commandDictionary.TryGetValue(control.Id, out cmd))
            {
                Dispatcher.Execute(cmd);
            }
            else
            {
                throw new NotImplementedException("No matching command for " + control.Id);
            }
        }

        /// <summary>
        /// Returns an Image object for the ribbon.
        /// </summary>
        /// <remarks>
        /// The image file is expected to be a WPF resource file, not an embedded resource.
        /// To be consistent accross the application which uses WPF resources for its WPF
        /// windows, all images are to be built as resources rather than embedded resources.
        /// </remarks>
        /// <param name="imageId">The file name (without path) of the image.</param>
        /// <returns>Image object</returns>
        public object Ribbon_LoadImage(string imageId)
        {
            string initPackScheme = System.IO.Packaging.PackUriHelper.UriSchemePack;
            StreamResourceInfo sri = Application.GetResourceStream(
                new Uri(@"pack://application:,,,/XLToolbox;component/Resources/images/" + imageId));
            return Image.FromStream(sri.Stream);
        }

        public string Control_GetLabel(Office.IRibbonControl control)
        {
            return LookupResourceString(control.Id + "Label");
        }

        public string Control_GetSupertip(Office.IRibbonControl control)
        {
            CustomUI.Button button = control as CustomUI.Button;
            string resourceName = control.Id + "Supertip";
            string supertip = null;
            if (button != null && !button.Enabled.Value)
            {
                supertip = LookupResourceString(resourceName + "Disabled");
            }
            if (String.IsNullOrEmpty(supertip))
            {
                supertip = LookupResourceString(resourceName);
            }
            return supertip;
        }

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            this._ribbon = ribbonUI;
        }

        public bool IsDebug(Office.IRibbonControl control)
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public bool ButtonCheckForUpdate_GetEnabled(Office.IRibbonControl control)
        {
            return (Versioning.UpdaterViewModel.Instance.CanCheckForUpdate);
        }

        public bool HasWorkbook(Office.IRibbonControl control)
        {
            // Use || for short-circuit evaluation to avoid null reference errors.
            return !(ExcelApp == null || ExcelApp.ActiveWorkbook == null);
        }

        public bool HasSelection(Office.IRibbonControl control)
        {
            // Use || for short-circuit evaluation to avoid null reference errors.
            return !(ExcelApp == null || ExcelApp.Selection == null);
        }

        #endregion

        #region Event handlers

        void UpdaterViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _ribbon.InvalidateControl("ButtonCheckForUpdate");
        }

        void Excel_WorkbookEvent(Xl.Workbook Wb)
        {
            _ribbon.Invalidate();
        }

        void _excelApp_SheetSelectionChange(object Sh, Xl.Range Target)
        {
            _ribbon.Invalidate();
        }

        #endregion

        #region Properties

        public Xl.Application ExcelApp
        {
            get
            {
                return _excelApp;
            }
            set
            {
                _excelApp = value;
                _excelApp.WorkbookActivate += Excel_WorkbookEvent;
                _excelApp.WorkbookDeactivate += Excel_WorkbookEvent;
                _excelApp.SheetSelectionChange += _excelApp_SheetSelectionChange;
                _ribbon.Invalidate();
            }
        }

        #endregion

        #region Helpers

        private string LookupResourceString(string name)
        {
            return RibbonStrings.ResourceManager.GetString(name);
        }

        private static string GetResourceText(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; ++i)
            {
                if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                    {
                        if (resourceReader != null)
                        {
                            return resourceReader.ReadToEnd();
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Fields

        Office.IRibbonUI _ribbon;
        Dictionary<string, Command> _commandDictionary;
        Microsoft.Office.Interop.Excel.Application _excelApp;

        #endregion
    }
}