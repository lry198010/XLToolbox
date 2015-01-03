﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XLToolbox.Export.Models
{
    /// <summary>
    /// Holds settings for a specific single export process.
    /// </summary>
    public class SingleExportSettings : Settings
    {
        #region Public properties

        /// <summary>
        /// Width of the selection in points.
        /// </summary>
        public double Width
        {
            get { return _width; }
            set
            {
                // Preserve aspect, but only if current dimensions are not 0
                if (PreserveAspect && _width != 0 && _height != 0)
                {

                    double _aspect = _height / _width;
                    _height = value * _aspect;
                }
                _width = value;
            }
        }

        /// <summary>
        /// Height of the selection in points.
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                if (PreserveAspect && _width != 0 && _height != 0)
                {
                    double _aspect = _width / _height;
                    _width = value * _aspect;
                }
                _height = value;
            }
        }

        public bool PreserveAspect { get; set; }

        public Unit Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                bool oldAspect = PreserveAspect;
                PreserveAspect = false;
                Height = _unit.ConvertTo(Height, value);
                Width = _unit.ConvertTo(Width, value);
                _unit = value;
                PreserveAspect = oldAspect;
            }
        }

        #endregion

        #region Constructors

        public SingleExportSettings()
            : base()
        {
            _unit = Models.Unit.Point;
            PreserveAspect = true;
        }

        public SingleExportSettings(Preset preset, double width, double height, bool preserveAspect)
            : this()
        {
            Preset = preset;
            Width = width;
            Height = height;
            PreserveAspect = preserveAspect;
        }

        #endregion

        #region Private fields

        double _width;
        double _height;
        Unit _unit;

        #endregion
    }
}
