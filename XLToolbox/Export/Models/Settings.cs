﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using XLToolbox.Excel.ViewModels;
using System.Configuration;

namespace XLToolbox.Export.Models
{
    /// <summary>
    /// Holds settings for a particular graphic export process, including
    /// dimensions of the resulting graphic.
    /// </summary>
    [Serializable]
    public abstract class Settings
    {
        #region Public properties

        public Preset Preset { get; set; }
        public string FileName { get; set; }

        #endregion

        #region Constructors

        public Settings() { }

        #endregion
    }
}
