﻿// FFXIV TexTools
// Copyright © 2019 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using FFXIV_TexTools.Helpers;
using FFXIV_TexTools.Properties;
using FFXIV_TexTools.Resources;
using FFXIV_TexTools.Views.Models;
using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Items.Interfaces;
using xivModdingFramework.Models.DataContainers;
using xivModdingFramework.Models.FileTypes;
using Path = System.IO.Path;

namespace FFXIV_TexTools.ViewModels
{
    public class AdvancedImportViewModel : INotifyPropertyChanged
    {
        private Dae _dae;
        private XivMdl _xivMdl;
        private LevelOfDetail _lod;
        private readonly IItemModel _itemModel;
        private readonly XivRace _selectedRace;
        private List<string> _boneList,  _shapesList;
        private List<int> _meshNumbers, _partNumbers;
        private ObservableCollection<string> _materialsList, _materialUsedList, _partAttributeList, _attributeList;
        private string _materialsGroupHeader, _attributesGroupHeader, _boneGroupHeader, _shapesHeader, _shapeDescription;
        private string _selectedMaterial, _selectedAttribute, _selectedMaterialUsed, _selectedPartAttribute, _selectedAvailablePartAttribute;
        private string _materialText, _attributeText, _partCountLabel, _partAttributesLabel, _daeLocationText;
        private int _selectedMeshNumber, _selectedMeshNumberIndex, _selectedPartNumber, _selectedPartNumberIndex, _selectedMaterialUsedIndex;
        private bool _shapeDataCheckBoxEnabled, _disableShapeDataChecked;
        private readonly string _textColor = "Black";
        private Dictionary<string, string> _attributeDictionary, _shapeDictionary;
        private Dictionary<string, int> _attributeMaskDictionary;
        private Dictionary<int, List<int>> _daeMeshPartDictionary;
        private readonly AdvancedModelImportView _view;
        private Dictionary<string, ModelImportSettings> _importDictionary = new Dictionary<string, ModelImportSettings>();


        public AdvancedImportViewModel(XivMdl xivMdl, IItemModel itemModel, XivRace selectedRace, AdvancedModelImportView view)
        {
            var appStyle = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            if (appStyle.Item1.Name.Equals("BaseDark"))
            {
                _textColor = "White";
            }

            _view = view;
            _xivMdl = xivMdl;
            _lod = xivMdl.LoDList[0];
            _itemModel = itemModel;
            _selectedRace = selectedRace;
            _dae = new Dae(new DirectoryInfo(Settings.Default.FFXIV_Directory), itemModel.DataFile);
            Initialize();
        }

        /// <summary>
        /// Initialize Advanced Import
        /// </summary>
        private void Initialize()
        {
            MaterialsList = new ObservableCollection<string>(_xivMdl.PathData.MaterialList);
            AttributeList = new ObservableCollection<string>(MakeAttributeNameDictionary());
            BoneList      = _xivMdl.PathData.BoneList;

            MaterialsGroupHeader  = $"Materials (Count: {MaterialsList.Count})";
            AttributesGroupHeader = $"Attributes (Count: {AttributeList.Count})";
            BonesGroupHeader      = $"Bones (Count: {BoneList.Count})";

            MaterialUsed = MaterialsList;

            var meshNumberList = new List<int>();
            for (var i = 0; i < _lod.MeshDataList.Count; i++)
            {
                meshNumberList.Add(i);
            }

            MeshNumbers = meshNumberList;

            var saveDir = new DirectoryInfo(Settings.Default.Save_Directory);
            var path = $"{IOUtil.MakeItemSavePath(_itemModel, saveDir, _selectedRace)}\\3D";
            var modelName = Path.GetFileNameWithoutExtension(_xivMdl.MdlPath.File);
            var savePath = new DirectoryInfo(Path.Combine(path, modelName) + ".dae");

            if (File.Exists(savePath.FullName))
            {
                DaeLocationText = savePath.FullName;

                _daeMeshPartDictionary = _dae.QuickColladaReader(savePath);
            }

            foreach (var meshNum in _daeMeshPartDictionary.Keys)
            {
                _importDictionary.Add(meshNum.ToString(), new ModelImportSettings{PartList = _daeMeshPartDictionary[meshNum]});
            }

            SelectedMeshNumberIndex = 0;
        }


        #region Properties

        /// <summary>
        /// Header for Materials Group
        /// </summary>
        public string MaterialsGroupHeader
        {
            get => _materialsGroupHeader;
            set
            {
                _materialsGroupHeader = value;
                NotifyPropertyChanged(nameof(MaterialsGroupHeader));
            }
        }

        /// <summary>
        /// Header for Attributes Group
        /// </summary>
        public string AttributesGroupHeader
        {
            get => _attributesGroupHeader;
            set
            {
                _attributesGroupHeader = value;
                NotifyPropertyChanged(nameof(AttributesGroupHeader));
            }
        }

        /// <summary>
        /// Header for Bones Group
        /// </summary>
        public string BonesGroupHeader
        {
            get => _boneGroupHeader;
            set
            {
                _boneGroupHeader = value;
                NotifyPropertyChanged(nameof(BonesGroupHeader));
            }
        }

        /// <summary>
        /// Part Count Label
        /// </summary>
        public string PartCountLabel
        {
            get => _partCountLabel;
            set
            {
                _partCountLabel = value;
                NotifyPropertyChanged(nameof(PartCountLabel));
            }
        }

        /// <summary>
        /// List of Material strings
        /// </summary>
        public ObservableCollection<string> MaterialsList
        {
            get => _materialsList;
            set
            {
                _materialsList = value;
                NotifyPropertyChanged(nameof(MaterialsList));

            }
        }

        /// <summary>
        /// List of attribute strings
        /// </summary>
        public ObservableCollection<string> AttributeList
        {
            get => _attributeList;
            set
            {
                _attributeList = value;
                NotifyPropertyChanged(nameof(AttributeList));

            }
        }

        /// <summary>
        /// List of bone strings
        /// </summary>
        public List<string> BoneList
        {
            get => _boneList;
            set
            {
                _boneList = value;
                NotifyPropertyChanged(nameof(BoneList));

            }
        }

        /// <summary>
        /// Selected Material
        /// </summary>
        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                MaterialStringText = value;
                NotifyPropertyChanged(nameof(SelectedMaterial));
            }
        }

        /// <summary>
        /// Selected Attribute
        /// </summary>
        public string SelectedAttribute
        {
            get => _selectedAttribute;
            set
            {
                _selectedAttribute = value;

                if(value != null)
                {
                    AttributeStringText = _attributeDictionary[value];
                }

                NotifyPropertyChanged(nameof(SelectedAttribute));
            }
        }

        /// <summary>
        /// String in Material TextBox
        /// </summary>
        public string MaterialStringText
        {
            get => _materialText;
            set
            {
                _materialText = value;
                NotifyPropertyChanged(nameof(MaterialStringText));
            }
        }

        /// <summary>
        /// String in Attribute TextBox
        /// </summary>
        public string AttributeStringText
        {
            get => _attributeText;
            set
            {
                _attributeText = value;
                NotifyPropertyChanged(nameof(AttributeStringText));
            }
        }

        /// <summary>
        /// List of Mesh Numbers
        /// </summary>
        public List<int> MeshNumbers
        {
            get => _meshNumbers;
            set
            {
                _meshNumbers = value;
                NotifyPropertyChanged(nameof(MeshNumbers));
            }
        }

        /// <summary>
        /// Selected Mesh Number
        /// </summary>
        public int SelectedMeshNumber
        {
            get => _selectedMeshNumber;
            set
            {
                _selectedMeshNumber = value;
                UpdatePartNumbers();
                UpdateMaterialUsed();
                UpdateShapes();
                NotifyPropertyChanged(nameof(SelectedMeshNumber));
            }
        }

        /// <summary>
        /// Selected Mesh Number Index
        /// </summary>
        public int SelectedMeshNumberIndex
        {
            get => _selectedMeshNumberIndex;
            set
            {
                _selectedMeshNumberIndex = value;
                UpdatePartNumbers();
                UpdateMaterialUsed();
                UpdateShapes();
                NotifyPropertyChanged(nameof(SelectedMeshNumberIndex));
            }
        }

        /// <summary>
        /// Part Numbers
        /// </summary>
        public List<int> PartNumbers
        {
            get => _partNumbers;
            set
            {
                _partNumbers = value;
                NotifyPropertyChanged(nameof(PartNumbers));
            }
        }

        /// <summary>
        /// Selected Part Number
        /// </summary>
        public int SelectedPartNumber
        {
            get => _selectedPartNumber;
            set
            {
                _selectedPartNumber = value;
                UpdateAttributesUsed();
                NotifyPropertyChanged(nameof(SelectedPartNumber));
            }
        }

        /// <summary>
        /// Selected Part Number Index
        /// </summary>
        public int SelectedPartNumberIndex
        {
            get => _selectedPartNumberIndex;
            set
            {
                _selectedPartNumberIndex = value;
                UpdateAttributesUsed();
                NotifyPropertyChanged(nameof(SelectedPartNumberIndex));
            }
        }

        /// <summary>
        /// List of Material Used
        /// </summary>
        public ObservableCollection<string> MaterialUsed
        {
            get => _materialUsedList;
            set
            {
                _materialUsedList = value;
                NotifyPropertyChanged(nameof(MaterialUsed));

            }
        }

        /// <summary>
        /// Selected Material Used
        /// </summary>
        public string SelectedMaterialUsed
        {
            get => _selectedMaterialUsed;
            set
            {
                _selectedMaterialUsed = value;
                UpdateMdlMaterialUsed();
                NotifyPropertyChanged(nameof(SelectedMaterialUsed));
            }
        }

        /// <summary>
        /// Selected Material Used Index
        /// </summary>
        public int SelectedMaterialUsedIndex
        {
            get => _selectedMaterialUsedIndex;
            set
            {
                _selectedMaterialUsedIndex = value;
                NotifyPropertyChanged(nameof(SelectedMaterialUsedIndex));
            }
        }

        /// <summary>
        /// List of part attributes
        /// </summary>
        public ObservableCollection<string> PartAttributes
        {
            get => _partAttributeList;
            set
            {
                _partAttributeList = value;
                NotifyPropertyChanged(nameof(PartAttributes));
            }
        }

        /// <summary>
        /// Selected part attribute
        /// </summary>
        public string SelectedPartAttribute
        {
            get => _selectedPartAttribute;
            set
            {
                _selectedPartAttribute = value;
                NotifyPropertyChanged(nameof(SelectedPartAttribute));
            }
        }

        /// <summary>
        /// Selected Available attribute
        /// </summary>
        public string SelectedAvailableAttribute
        {
            get => _selectedAvailablePartAttribute;
            set
            {
                _selectedAvailablePartAttribute = value;
                NotifyPropertyChanged(nameof(SelectedAvailableAttribute));
            }
        }

        /// <summary>
        /// Part Attribute label
        /// </summary>
        public string PartAttributesLabel
        {
            get => _partAttributesLabel;
            set
            {
                _partAttributesLabel = value;
                NotifyPropertyChanged(nameof(PartAttributesLabel));
            }
        }

        /// <summary>
        /// DAE directory string in Location TextBox
        /// </summary>
        public string DaeLocationText
        {
            get => _daeLocationText;
            set
            {
                _daeLocationText = value;
                NotifyPropertyChanged(nameof(DaeLocationText));
            }
        }

        /// <summary>
        /// Shape description
        /// </summary>
        public string ShapeDescription
        {
            get => _shapeDescription;
            set
            {
                _shapeDescription = value;
                NotifyPropertyChanged(nameof(ShapeDescription));
            }
        }

        /// <summary>
        /// List of shapes
        /// </summary>
        public List<string> ShapesList
        {
            get => _shapesList;
            set
            {
                _shapesList = value;
                NotifyPropertyChanged(nameof(ShapesList));
            }
        }

        /// <summary>
        /// Shape Data Check Box Enabled Status
        /// </summary>
        public bool ShapeDataCheckBoxEnabled
        {
            get => _shapeDataCheckBoxEnabled;
            set
            {
                _shapeDataCheckBoxEnabled = value;
                NotifyPropertyChanged(nameof(ShapeDataCheckBoxEnabled));
            }
        }

        /// <summary>
        /// Shapes Header
        /// </summary>
        public string ShapesHeader
        {
            get => _shapesHeader;
            set
            {
                _shapesHeader = value;
                NotifyPropertyChanged(nameof(ShapesHeader));
            }
        }

        /// <summary>
        /// Status of Shape Data CheckBox
        /// </summary>
        public bool DisableShapeDataChecked
        {
            get => _disableShapeDataChecked;
            set
            {
                _disableShapeDataChecked = value;
                UpdateImportDictionary();
                NotifyPropertyChanged(nameof(DisableShapeDataChecked));
            }
        }

        #endregion

        #region Commands

        public ICommand FileSelectCommand => new RelayCommand(SelectDaeFile);
        public ICommand AddRemoveMaterialCommand => new RelayCommand(AddRemoveMaterial);
        public ICommand AddRemoveAttributeCommand => new RelayCommand(AddRemoveAttribute);
        public ICommand AddPartAttributeCommand => new RelayCommand(AddPartAttribute);
        public ICommand RemovePartAttributeCommand => new RelayCommand(RemovePartAttribute);
        public ICommand ImportCommand => new RelayCommand(Import);

        #endregion

        #region Private Methods

        /// <summary>
        /// Update Part Numbers
        /// </summary>
        private void UpdatePartNumbers()
        {
            var partNumberList = new List<int>();
            var originalPartListCount = _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count;
            var daePartList = _daeMeshPartDictionary[SelectedMeshNumber];
            var partDiff = "";

            if (daePartList.Count > originalPartListCount)
            {
                partDiff = $"(+{daePartList.Count - originalPartListCount})";
                for (var i = 0; i < daePartList.Count; i++)
                {
                    partNumberList.Add(i);
                }
            }
            else
            {
                if (daePartList.Count < originalPartListCount)
                {
                    partDiff = $"(-{originalPartListCount - daePartList.Count})";
                }

                for (var i = 0; i < _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count; i++)
                {
                    partNumberList.Add(i);
                }
            }

            PartNumbers = partNumberList;
            PartCountLabel = $"Part Count: {PartNumbers.Count} {partDiff}";
            SelectedPartNumberIndex = 0;

            CheckForDaeDiscrepancy();
        }

        /// <summary>
        /// Update Material Used
        /// </summary>
        private void UpdateMaterialUsed()
        {
            var materialIndex = _lod.MeshDataList[SelectedMeshNumber].MeshInfo.MaterialIndex;

            SelectedMaterialUsedIndex = materialIndex;
        }

        /// <summary>
        /// Update Mdl Material Used
        /// </summary>
        private void UpdateMdlMaterialUsed()
        {
            // Change the XivMdl data directly for material index if the mesh exists, otherwise add it to the import settings
            if (SelectedMeshNumber > _lod.MeshDataList.Count)
            {
                _importDictionary[SelectedMeshNumber.ToString()].MaterialIndex = (short)SelectedMaterialUsedIndex;
            }
            else
            {
                _lod.MeshDataList[SelectedMeshNumber].MeshInfo.MaterialIndex = (short)SelectedMaterialUsedIndex;
            }
        }

        /// <summary>
        /// Update Attribute Used
        /// </summary>
        private void UpdateAttributesUsed()
        {
            var attributeMask = _lod.MeshDataList[SelectedMeshNumber].MeshPartList[SelectedPartNumber].AttributeIndex;

            var attributeNameList = new List<string>();

            for (var i = 0; i < AttributeList.Count; i++)
            {
                var value = 1 << i;
                if ((attributeMask & value) > 0)
                {
                    attributeNameList.Add($"{AttributeList[i]}");
                }
            }

            PartAttributes = new ObservableCollection<string>(attributeNameList);

            PartAttributesLabel = $"Part Attributes (Count: {PartAttributes.Count})";
        }

        /// <summary>
        /// Update Shapes
        /// </summary>
        private void UpdateShapes()
        {
            MakeShapeNameDictionary();

            var shapePathList = new List<string>();

            if (_lod.MeshDataList[SelectedMeshNumber].ShapePathList != null)
            {
                foreach (var shapePath in _lod.MeshDataList[SelectedMeshNumber].ShapePathList)
                {
                    shapePathList.Add(_shapeDictionary[shapePath]);
                }
            }

            //ShapeDataCheckBoxEnabled = shapePathList.Count >= 1;

            if (ShapeDataCheckBoxEnabled)
            {
                ShapeDescription =
                    "This will disable all shape data for all meshes.\n" +
                    "This option is used when holes appear upon equipping other items\n\n" +
                    "More options for shape data will be available in a later version.";
            }
            else
            {
                ShapeDescription = "There is no Shape Data for this mesh.\n\n" +
                                   "Options are disabled.";
            }

            ShapesList = shapePathList;

            ShapesHeader = $"Shapes (Count: {ShapesList.Count})";
        }

        /// <summary>
        /// Update Import Dictionary
        /// </summary>
        private void UpdateImportDictionary()
        {
            if (_importDictionary.ContainsKey(SelectedMeshNumber.ToString()))
            {
                _importDictionary[SelectedMeshNumber.ToString()].Disable = DisableShapeDataChecked;
            }
        }

        /// <summary>
        /// Check DAE for discrepancies
        /// </summary>
        private void CheckForDaeDiscrepancy()
        {
            _view.DaeInfoTextBox.Document.Blocks.Clear();

            var meshCount = _daeMeshPartDictionary.Count;

            // Check for mesh difference
            if (meshCount > _lod.MeshDataList.Count)
            {
                var extraCount = meshCount - _lod.MeshDataList.Count;
                AddText($"{extraCount}", "Green", true);
                AddText($" added mesh(es)\nChange material for new mesh(es) if necessary.\n\n", _textColor, false);

                // Update mesh number list
                var meshNumberList = new List<int>();
                for (var i = 0; i < meshCount; i++)
                {
                    meshNumberList.Add(i);
                }

                MeshNumbers = meshNumberList;
            }
            else if (meshCount < _lod.MeshDataList.Count)
            {
                var removedCount = _lod.MeshDataList.Count - meshCount;
                AddText($"{removedCount}", "Red", true);
                AddText($" removed mesh(es)\nRemoved Mesh Number(s): ", _textColor, false);

                foreach (var meshNumber in MeshNumbers)
                {
                    if (!_daeMeshPartDictionary.ContainsKey(meshNumber))
                    {
                        AddText($"{meshNumber} ", "Red", true);
                    }
                }

                AddText("\nChanges to these removed meshes above will have no effect\n\n", _textColor, true);
            }
            else
            {
                AddText("No difference in mesh counts.\n\n\n", _textColor, false);
            }

            // Check for mesh part difference
            if (_daeMeshPartDictionary.ContainsKey(SelectedMeshNumber))
            {
                var meshPartList = _daeMeshPartDictionary[SelectedMeshNumber];

                if (meshPartList.Count > _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count)
                {
                    var extraCount = meshPartList.Count - _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count;
                    AddText($"{extraCount}", "Green", true);
                    AddText(" added mesh part(s) for this mesh\nChange attributes for new part(s) below if necessary", _textColor, false);
                }
                else if(meshPartList.Count < _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count)
                {
                    var removedCount = _lod.MeshDataList[SelectedMeshNumber].MeshPartList.Count - meshPartList.Count;
                    AddText($"{removedCount}", "Red", true);
                    AddText(" removed mesh part(s) for this mesh\nRemoved Part Number(s): ", _textColor, false);

                    foreach (var partNumber in PartNumbers)
                    {
                        if (!meshPartList.Contains(partNumber))
                        {
                            AddText($"{partNumber} ", "Red", true);
                        }
                    }

                    AddText("\nChanges to these removed parts below will have no effect", _textColor, false);
                }
                else
                {
                    AddText("No difference in mesh part counts for this mesh.", _textColor, false);
                }
            }
        }

        /// <summary>
        /// Event Handler for Add/Remove Material Button
        /// </summary>
        private void AddRemoveMaterial(object obj)
        {
            if (MaterialsList.Contains(MaterialStringText))
            {
                var materialIndex = MaterialsList.IndexOf(MaterialStringText);
                var materialMdlIndex = _xivMdl.PathData.MaterialList.IndexOf(MaterialStringText);

                MaterialsList.RemoveAt(materialIndex);
                _xivMdl.PathData.MaterialList.RemoveAt(materialMdlIndex);
                _xivMdl.ModelData.MaterialCount -= 1;
                _xivMdl.PathData.PathCount -= 1;
                _xivMdl.PathData.PathBlockSize -= MaterialStringText.Length + 1;
            }
            else
            {
                if (!string.IsNullOrEmpty(MaterialStringText))
                {
                    MaterialsList.Add(MaterialStringText);
                    _xivMdl.PathData.MaterialList.Add(MaterialStringText);
                    _xivMdl.ModelData.MaterialCount += 1;
                    _xivMdl.PathData.PathCount += 1;
                    _xivMdl.PathData.PathBlockSize += MaterialStringText.Length + 1;
                }
            }

            MaterialsGroupHeader = $"Materials (Count: {MaterialsList.Count})";
        }

        /// <summary>
        /// Event Handler for Add/Remove Attribute Button
        /// </summary>
        private void AddRemoveAttribute(object obj)
        {
            if (_attributeDictionary.ContainsValue(AttributeStringText))
            {
                var key = _attributeDictionary.FirstOrDefault(x => x.Value == AttributeStringText).Key;

                var attributeIndex = AttributeList.IndexOf(key);
                var attributeMdlIndex = _xivMdl.PathData.AttributeList.IndexOf(AttributeStringText);

                AttributeList.RemoveAt(attributeIndex);
                _xivMdl.PathData.AttributeList.RemoveAt(attributeMdlIndex);
                _xivMdl.ModelData.AttributeCount -= 1;
                _xivMdl.PathData.PathCount -= 1;
                _xivMdl.PathData.PathBlockSize -= AttributeStringText.Length + 1;
            }
            else
            {
                if (!string.IsNullOrEmpty(AttributeStringText))
                {
                    AttributeList.Add(AttributeStringText);
                    _xivMdl.PathData.AttributeList.Add(AttributeStringText);
                    _xivMdl.ModelData.AttributeCount += 1;
                    _xivMdl.PathData.PathCount += 1;
                    _xivMdl.PathData.PathBlockSize += AttributeStringText.Length + 1;
                }
            }

            AttributeList = new ObservableCollection<string>(MakeAttributeNameDictionary());

            AttributesGroupHeader = $"Attributes (Count: {AttributeList.Count})";
        }

        /// <summary>
        /// Event Handler to add a part attribute
        /// </summary>
        private void AddPartAttribute(object obj)
        {
            if (string.IsNullOrEmpty(SelectedAvailableAttribute)) return;

            var attributeMask = _lod.MeshDataList[SelectedMeshNumber].MeshPartList[SelectedPartNumber].AttributeIndex;

            attributeMask += _attributeMaskDictionary[SelectedAvailableAttribute];

            _lod.MeshDataList[SelectedMeshNumber].MeshPartList[SelectedPartNumber].AttributeIndex = attributeMask;

            PartAttributes.Add(SelectedAvailableAttribute);
        }

        /// <summary>
        /// Event Handler to remove a part attribute
        /// </summary>
        private void RemovePartAttribute(object obj)
        {
            if (string.IsNullOrEmpty(SelectedPartAttribute)) return;

            var attributeMask = _lod.MeshDataList[SelectedMeshNumber].MeshPartList[SelectedPartNumber].AttributeIndex;

            attributeMask -= _attributeMaskDictionary[SelectedPartAttribute];

            _lod.MeshDataList[SelectedMeshNumber].MeshPartList[SelectedPartNumber].AttributeIndex = attributeMask;

            var attributeIndex = PartAttributes.IndexOf(SelectedPartAttribute);

            PartAttributes.RemoveAt(attributeIndex);
        }

        /// <summary>
        /// Event Handler for Import
        /// </summary>
        private void Import(object obj)
        {
            var mdl = new Mdl(new DirectoryInfo(Settings.Default.FFXIV_Directory), _itemModel.DataFile);

            mdl.ImportModel(_itemModel, _xivMdl, new DirectoryInfo(DaeLocationText), _importDictionary,
                XivStrings.TexTools);
        }

        /// <summary>
        /// Make Attribute Name Dictionary
        /// </summary>
        /// <returns></returns>
        private List<string> MakeAttributeNameDictionary()
        {
            _attributeDictionary = new Dictionary<string, string>();
            _attributeMaskDictionary = new Dictionary<string, int>();
            var nameList = new List<string>();

            foreach (var attribute in _xivMdl.PathData.AttributeList)
            {
                var hasNumber = attribute.Any(char.IsDigit);

                if (hasNumber)
                {
                    var attributeNumber = attribute.Substring(attribute.Length - 1);
                    var attributeName = attribute.Substring(0, attribute.Length - 1);

                    var name = $"{attribute} - {AttributeNameDictionary[attributeName]} {attributeNumber}";
                    nameList.Add(name);
                    _attributeDictionary.Add(name, attribute);
                }
                else
                {
                    if (attribute.Count(x => x == '_') > 1)
                    {
                        var attributeName = attribute.Substring(0, attribute.LastIndexOf("_"));
                        var attributePart = attribute.Substring(attribute.LastIndexOf("_") + 1, 1);

                        if (AttributeNameDictionary.ContainsKey(attributeName))
                        {
                            var name = $"{attribute} - {AttributeNameDictionary[attributeName]} {attributePart}";
                            nameList.Add(name);
                            _attributeDictionary.Add(name, attribute);
                        }
                        else
                        {
                            nameList.Add($"{attribute}");
                            _attributeDictionary.Add(attribute, attribute);
                        }
                    }
                    else
                    {
                        if (AttributeNameDictionary.ContainsKey(attribute))
                        {
                            var name = $"{attribute} - {AttributeNameDictionary[attribute]}";
                            nameList.Add(name);
                            _attributeDictionary.Add(name, attribute);
                        }
                        else
                        {
                            nameList.Add(attribute);
                            _attributeDictionary.Add(attribute, attribute);
                        }
                    }
                }

            }

            var mask = 1;
            foreach (var attribute in nameList)
            {
                _attributeMaskDictionary.Add(attribute, mask);

                mask *= 2;
            }

            return nameList;
        }

        /// <summary>
        /// Make Shape Name Dictionary
        /// </summary>
        private void MakeShapeNameDictionary()
        {
            _shapeDictionary = new Dictionary<string, string>();
            var nameList = new List<string>();

            foreach (var shape in _xivMdl.PathData.ShapeList)
            {
                var hasNumber = shape.Any(char.IsDigit);

                if (hasNumber)
                {
                    var shapeNumber = shape.Substring(shape.Length - 1);
                    var shapeName = shape.Substring(0, shape.Length - 1);

                    var name = $"{shape} - {ShapeNameDictionary[shapeName]} {shapeNumber}";
                    nameList.Add(name);
                    _shapeDictionary.Add(shape, name);
                }
                else
                {
                    if (shape.Count(x => x == '_') > 1)
                    {
                        var shapeName = shape.Substring(0, shape.LastIndexOf("_"));
                        var shapePart = shape.Substring(shape.LastIndexOf("_") + 1, 1);

                        if (ShapeNameDictionary.ContainsKey(shapeName))
                        {
                            var name = $"{shape} - {ShapeNameDictionary[shapeName]} {shapePart}";
                            nameList.Add(name);
                            _shapeDictionary.Add(shape, name);
                        }
                        else
                        {
                            nameList.Add($"{shape}");
                            _shapeDictionary.Add(shape, shape);
                        }
                    }
                    else
                    {
                        if (ShapeNameDictionary.ContainsKey(shape))
                        {
                            var name = $"{shape} - {ShapeNameDictionary[shape]}";
                            nameList.Add(name);
                            _shapeDictionary.Add(shape, name);
                        }
                        else
                        {
                            nameList.Add(shape);
                            _shapeDictionary.Add(shape, shape);
                        }
                    }
                }

            }

            if (_shapeDictionary.Count > 0)
            {
                ShapeDataCheckBoxEnabled = true;
            }
        }

        /// <summary>
        /// Event handler for DAE file selector
        /// </summary>
        private void SelectDaeFile(object obj)
        {
            var saveDir = new DirectoryInfo(Settings.Default.Save_Directory);
            var path = new DirectoryInfo($"{IOUtil.MakeItemSavePath(_itemModel, saveDir, _selectedRace)}\\3D");

            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = path.FullName,
                Filter = "Collada DAE (*.dae)|*.dae"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DaeLocationText = openFileDialog.FileName;

                _daeMeshPartDictionary = _dae.QuickColladaReader(new DirectoryInfo(openFileDialog.FileName));
            }
        }

        /// <summary>
        /// Adds text to the text box
        /// </summary>
        /// <param name="text">The text to add</param>
        /// <param name="color">The color of the text</param>
        private void AddText(string text, string color, bool bold)
        {
            var bc = new BrushConverter();
            var tr = new TextRange(_view.DaeInfoTextBox.Document.ContentEnd, _view.DaeInfoTextBox.Document.ContentEnd) { Text = text };
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString(color));

                tr.ApplyPropertyValue(TextElement.FontWeightProperty, bold ? FontWeights.Bold : FontWeights.Normal);
            }
            catch (FormatException) { }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Attribute Name Dictionary
        /// </summary>
        private static readonly Dictionary<string, string> AttributeNameDictionary = new Dictionary<string, string>
        {
            {"none", "None" },
            {"atr_arm", "Arm"},
            {"atr_arrow", "Arrow"},
            {"atr_attach", "Attachment"},
            {"atr_hair", "Hair"},
            {"atr_hig", "Facial Hair"},
            {"atr_hij", "Lower Arm"},
            {"atr_hiz", "Upper Leg"},
            {"atr_hrn", "Horns"},
            {"atr_inr", "Neck"},
            {"atr_kam", "Hair"},
            {"atr_kao", "Face"},
            {"atr_kod", "Waist"},
            {"atr_leg", "Leg"},
            {"atr_lod", "LoD"},
            {"atr_lpd", "Feet Pads"},
            {"atr_mim", "Ear"},
            {"atr_nek", "Neck"},
            {"atr_sne", "Lower Leg"},
            {"atr_sta", "STA"},
            {"atr_tlh", "Tail Hide"},
            {"atr_tls", "Tail Show"},
            {"atr_top", "Top"},
            {"atr_ude", "Upper Arm"},
            {"atr_bv", "Body Part "},
            {"atr_dv", "Leg Part "},
            {"atr_mv", "Head Part "},
            {"atr_gv", "Hand Part "},
            {"atr_sv", "Feet Part "},
            {"atr_tv", "Top Part "},
            {"atr_fv", "Face Part "},
            {"atr_hv", "Hair Part "},
            {"atr_nv", "Neck Part "},
            {"atr_parts", "Part "},
            {"atr_rv", "RV Part "},
            {"atr_wv", "WV Part "},
            {"atr_ev", "EV Part "},
            {"atr_cn_ankle", "CN Ankle"},
            {"atr_cn_neck", "CN Neck"},
            {"atr_cn_waist", "CN Waist"},
            {"atr_cn_wrist", "CN Wrist"}
        };

        /// <summary>
        /// Shape Name Dictionary
        /// </summary>
        private static readonly Dictionary<string, string> ShapeNameDictionary = new Dictionary<string, string>
        {
            {"none", "None" },
            {"shp_hiz", "Upper Leg"},
            {"shp_kos", "Waist"},
            {"shp_mom", "Leg?"},
            {"shp_sne", "Lower Leg"},
            {"shp_leg", "Leg"},
            {"shp_sho", "Feet"},
            {"shp_arm", "Arm"},
            {"shp_kat", "Body?"},
            {"shp_hij", "Lower Arm"},
            {"shp_ude", "Upper Arm"},
            {"shp_nek", "Neck"},
            {"shp_brw", "Brow"},
            {"shp_chk", "Cheek"},
            {"shp_eye", "Eye"},
            {"shp_irs", "Iris"},
            {"shp_mth", "Mouth"},
            {"shp_nse", "Nose"}
        };
    }
}