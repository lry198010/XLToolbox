﻿/* SingleExportSettingsViewModel.cs
 * part of Daniel's XL Toolbox NG
 * 
 * Copyright 2014-2016 Daniel Kraus
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
using System.ComponentModel;
using Bovender.Mvvm;
using Bovender.Mvvm.Actions;
using Bovender.Mvvm.Messaging;
using XLToolbox.Excel.ViewModels;
using XLToolbox.Export.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XLToolbox.Export.ViewModels
{
    /// <summary>
    /// View model for the <see cref="Settings"/> class.
    /// </summary>
    public class SingleExportSettingsViewModel : SettingsViewModelBase
    {
        #region Public properties

        /// <summary>
        /// Gets or sets the desired width of the exported graphic.
        /// </summary>
        public double Width
        {
            get { return ((SingleExportSettings)Settings).Width; }
            set
            {
                ((SingleExportSettings)Settings).Width = value;
                _dimensionsChanged = true;
                OnPropertyChanged("Width");
                if (PreserveAspect) OnPropertyChanged("Height");
                OnPropertyChanged("MegaPixels");
                OnPropertyChanged("MegaPixelsWarning");
                OnPropertyChanged("MegaBytes");
                OnPropertyChanged("ImageSize");
            }
        }

        /// <summary>
        /// Gets or sets the desired width of the exported graphic.
        /// </summary>
        public double Height
        {
            get { return ((SingleExportSettings)Settings).Height; }
            set
            {
                ((SingleExportSettings)Settings).Height = value;
                _dimensionsChanged = true;
                OnPropertyChanged("Height");
                if (PreserveAspect) OnPropertyChanged("Width");
                OnPropertyChanged("MegaPixels");
                OnPropertyChanged("MegaPixelsWarning");
                OnPropertyChanged("MegaBytes");
                OnPropertyChanged("ImageSize");
            }
        }

        /// <summary>
        /// Returns an enumerable list of available units and provides
        /// a bindable converter for a WPF ComboBox.
        /// </summary>
        public EnumProvider<Unit> Units
        {
            get
            {
                if (_unitString == null)
                {
                    _unitString = new EnumProvider<Unit>();
                    _unitString.PropertyChanged +=
                        (object sender, PropertyChangedEventArgs args) =>
                        {
                            if (args.PropertyName == "AsEnum")
                            {
                                ((SingleExportSettings)Settings).Unit = Units.AsEnum;
                                OnPropertyChanged("Width");
                                OnPropertyChanged("Height");
                            }
                            OnPropertyChanged("Units." + args.PropertyName);
                        };
                }
                return _unitString;
            }
        }

        /// <summary>
        /// Preserve aspect ratio if width or height are changed.
        /// </summary>
        public bool PreserveAspect
        {
            get { return ((SingleExportSettings)Settings).PreserveAspect; }
            set
            {
                ((SingleExportSettings)Settings).PreserveAspect = value;
                OnPropertyChanged("PreserveAspect");
            }
        }

        /// <summary>
        /// Gets the number of megapixels for the resulting image.
        /// </summary>
        public double MegaPixels
        {
            get
            {
                if (SelectedPreset != null)
                {
                    int dpi = SelectedPreset.Dpi;
                    double mp = Units.AsEnum.ConvertTo(Width, Unit.Inch) * dpi * 
                        Units.AsEnum.ConvertTo(Height, Unit.Inch) * dpi;
                    return mp / 1000000;
                }
                else
                {
                    return -1;
                }
            }
        }

        public double MegaBytes
        {
            get
            {
                if (SelectedPreset != null)
	            {
                    return MegaPixels * SelectedPreset.ColorSpace.AsEnum.ToBPP() / 8 * 1000000 / (1024 * 1024);
	            }
                else
                {
                    return -1;
                }
            }
        }

        public string ImageSize
        {
            get
            {
                return String.Format(Strings.ImageSizeMegaPixels, MegaPixels, MegaBytes);
            }
        }

        public bool MegaPixelsWarning
        {
            get
            {
                return MegaPixels > 30;
            }
        }
        
        #endregion

        #region Commands

        /// <summary>
        /// Resets the Height and Width properties to the dimensions
        /// of the current selection in Excel.
        /// </summary>
        public DelegatingCommand ResetDimensionsCommand
        {
            get
            {
                if (_resetDimensionsCommand == null)
                {
                    _resetDimensionsCommand = new DelegatingCommand(
                        param => DoResetDimensions(),
                        param => CanResetDimensions()
                    );
                }
                return _resetDimensionsCommand;
            }
        }

        /// <summary>
        /// Causes the <see cref="ChooseFileNameMessage"/> to be sent.
        /// Upon confirmation of this message, the Export process will
        /// be started.
        /// </summary>
        public DelegatingCommand ChooseFileNameCommand
        {
            get
            {
                if (_chooseFileNameCommand == null)
                {
                    _chooseFileNameCommand = new DelegatingCommand(
                        param => DoChooseFileName(),
                        parma => CanChooseFileName());
                }
                return _chooseFileNameCommand;
            }
        }

        #endregion

        #region Messages

        public Message<FileNameMessageContent> ChooseFileNameMessage
        {
            get
            {
                if (_chooseFileNameMessage == null)
                {
                    _chooseFileNameMessage = new Message<FileNameMessageContent>();
                }
                return _chooseFileNameMessage;
            }
        }

        #endregion

        #region Constructors

        public SingleExportSettingsViewModel()
            : this(new SingleExportSettings())
        { }

        public SingleExportSettingsViewModel(SingleExportSettings singleExportSettings)
            : base(new Exporter(singleExportSettings))
        {
            Settings = singleExportSettings;
            PresetViewModels.Select(Settings.Preset);
            // Need to explicitly set the selected enum value in the EnumProvider<Unit> collection.
            Units.AsEnum = singleExportSettings.Unit;
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "SelectedPreset")
                {
                    OnPropertyChanged("MegaPixels");
                    OnPropertyChanged("MegaPixelsWarning");
                    OnPropertyChanged("MegaBytes");
                    OnPropertyChanged("ImageSize");
                }
            };
        }

        #endregion

        #region Implementation of SettingsViewModelBase and ProcessViewModelBase

        /// <summary>
        /// Determins the suggested target directory and sends the
        /// ChooseFileNameMessage.
        /// </summary>
        protected override void DoExport()
        {
            if (CanExport())
            {
                Logger.Info("DoExport");
                SelectedPreset.Store();
                UserSettings.UserSettings.Default.ExportUnit = Units.AsEnum;
                SaveExportPath();
                StartProcess();
            }
        }

        protected override bool CanExport()
        {
            SelectionViewModel svm = new SelectionViewModel(Instance.Default.Application);
            return (svm.Selection != null) && (SelectedPreset != null) &&
                (Settings.Preset != null) && (Settings.Preset.Dpi > 0) &&
                (Width > 0) && (Height > 0);
        }

        protected override bool BeforeStartProcess()
        {
            Settings.Preset = SelectedPreset.RevealModelObject() as Preset;
            return base.BeforeStartProcess();
        }

        protected override void UpdateProcessMessageContent(ProcessMessageContent processMessageContent)
        {
            if (Exporter != null)
            {
                processMessageContent.PercentCompleted = Exporter.PercentCompleted;
            }
        }

        #endregion

        #region Overrides

        protected override void DoEditPresets()
        {
            base.DoEditPresets();
            OnPropertyChanged("MegaBytes");
            OnPropertyChanged("MegaPixels");
            OnPropertyChanged("MegaPixelsWarning");
            OnPropertyChanged("ImageSize");
        }

        protected override void SaveExportPath()
        {
            base.SaveExportPath();
            UserSettings.UserSettings.Default.ExportPath =
                System.IO.Path.GetDirectoryName(FileName);
        }

        #endregion

        #region Private properties

        private Exporter Exporter
        {
            [DebuggerStepThrough]
            get
            {
                return ProcessModel as Exporter;
            }
        }
        
        #endregion

        #region Private methods

        private void DoChooseFileName()
        {
            Logger.Info("DoChooseFileName");
            if (CanChooseFileName())
            {
                Preset preset = SelectedPreset.RevealModelObject() as Preset;
                ChooseFileNameMessage.Send(
                    new FileNameMessageContent(
                        LoadExportPath(),
                        preset.FileType.ToFileFilter()
                        ),
                    (content) => DoConfirmFileName(content)
                );
            }
        }

        private bool CanChooseFileName()
        {
            return CanExport();
        }

        /// <summary>
        /// Called by Message.Respond() if the user has confirmed a file name
        /// in a view subscribed to the ChooseFileNameMessage. Triggers the
        /// actual export with the file name contained in the message content.
        /// </summary>
        /// <param name="messageContent"></param>
        private void DoConfirmFileName(FileNameMessageContent messageContent)
        {
            Logger.Info("DoConfirmFileName");
            if (messageContent.Confirmed)
            {
                Logger.Info("Confirmed");
                ((SingleExportSettings)Settings).FileName = messageContent.Value;
                UserSettings.UserSettings.Default.ExportPath =
                    System.IO.Path.GetDirectoryName(messageContent.Value);
                DoExport();
            }
            else
            {
                Logger.Info("Not confirmed");
            }
        }

        private void DoResetDimensions()
        {
            Logger.Info("DoResetDimensions");
            if (CanResetDimensions())
            {
                SelectionViewModel selection = new SelectionViewModel(
                    Instance.Default.Application);
                bool oldAspectSwitch = PreserveAspect;
                PreserveAspect = false;
                Width = Unit.Point.ConvertTo(selection.Bounds.Width, Units.AsEnum);
                Height = Unit.Point.ConvertTo(selection.Bounds.Height, Units.AsEnum);
                PreserveAspect = oldAspectSwitch;
                _dimensionsChanged = false;
            }
        }

        private bool CanResetDimensions()
        {
            return _dimensionsChanged;
        }

        #endregion

        #region Private fields

        private DelegatingCommand _chooseFileNameCommand;
        private DelegatingCommand _resetDimensionsCommand;
        private Message<FileNameMessageContent> _chooseFileNameMessage;
        private EnumProvider<Unit> _unitString;
        private bool _dimensionsChanged;

        #endregion
    }
}
