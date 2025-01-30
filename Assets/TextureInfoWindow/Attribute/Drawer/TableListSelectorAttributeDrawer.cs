using System;
using System.Collections.Generic;
using System.Linq;
using ARK.EditorTools.CustomAttribute;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace ARK.EditorTools.Image
{
    public class TableListSelectorAttributeDrawer : OdinAttributeDrawer<TableListSelectorAttribute>
    {

        private IOrderedCollectionResolver      resolver;
        private LocalPersistentContext<bool>    isPagingExpanded;
        private LocalPersistentContext<Vector2> scrollPos;
        private LocalPersistentContext<int>     currPage;
        private GUITableRowLayoutGroup          table;
        private HashSet<string>                 seenColumnNames;
        private List<Column>                    columns;
        private ObjectPicker                    picker;
        private int                             colOffset;
        private GUIContent                      indexLabel;
        private bool                            isReadOnly;
        private int                             indexLabelWidth;
        private Rect                            columnHeaderRect;
        private GUIPagingHelper                 paging;
        private bool                            drawAsList;
        private bool                            isFirstFrame = true;

        ///
        ///
        private bool isPressCtrl;
        private        bool                               isPressShift;
        private static Color                              selectedColor = new Color(0.301f, 0.563f, 1f, 0.497f);
        private        bool                               isListElement;
        private        InspectorProperty                  baseMemberProperty;
        private        PropertyContext<InspectorProperty> globalSelectedProperty;
        private        InspectorProperty                  selectedProperty;
        private        List<InspectorProperty>            selectedPropertyList;
        private        Action<object, List<int>>          selectedIndexSetter;
        ///////

        ///
        /// <summary>
        /// Determines whether this instance [can draw attribute property] the specified property.
        /// </summary>
        protected override bool CanDrawAttributeProperty(InspectorProperty property) => property.ChildResolver is IOrderedCollectionResolver;

        /// <summary>Initializes this instance.</summary>
        protected override void Initialize()
        {
            this.drawAsList = false;
            this.isReadOnly = this.Attribute.IsReadOnly || !this.Property.ValueEntry.IsEditable;
            this.indexLabelWidth = (int)SirenixGUIStyles.Label.CalcSize(new GUIContent("100")).x + 15;
            this.indexLabel = new GUIContent();
            this.colOffset = 0;
            this.seenColumnNames = new HashSet<string>();
            this.table = new GUITableRowLayoutGroup();
            this.table.MinScrollViewHeight = this.Attribute.MinScrollViewHeight;
            this.table.MaxScrollViewHeight = this.Attribute.MaxScrollViewHeight;
            this.resolver = this.Property.ChildResolver as IOrderedCollectionResolver;
            this.scrollPos = this.GetPersistentValue<Vector2>("scrollPos", Vector2.zero);
            this.currPage = this.GetPersistentValue<int>("currPage");
            this.isPagingExpanded = this.GetPersistentValue<bool>("expanded");
            this.columns = new List<Column>(10);
            this.paging = new GUIPagingHelper();
            this.paging.NumberOfItemsPerPage = this.Attribute.NumberOfItemsPerPage > 0 ? this.Attribute.NumberOfItemsPerPage : GlobalConfig<GeneralDrawerConfig>.Instance.NumberOfItemsPrPage;
            this.paging.IsExpanded = this.isPagingExpanded.Value;
            this.paging.IsEnabled = GlobalConfig<GeneralDrawerConfig>.Instance.ShowPagingInTables || this.Attribute.ShowPaging;
            this.paging.CurrentPage = this.currPage.Value;
            this.Property.ValueEntry.OnChildValueChanged += new Action<int>(this.OnChildValueChanged);
            if(this.Attribute.AlwaysExpanded)
                this.Property.State.Expanded = true;
            int cellPadding = this.Attribute.CellPadding;
            if(cellPadding > 0)
                this.table.CellStyle = new GUIStyle()
                {
                    padding = new RectOffset(cellPadding, cellPadding, cellPadding, cellPadding)
                };
            GUIHelper.RequestRepaint(3);
            if(this.Attribute.ShowIndexLabels)
            {
                ++this.colOffset;
                this.columns.Add(new Column(this.indexLabelWidth, true, false, (string)null, ColumnType.Index));
            }
            if(this.isReadOnly)
                return;
            this.columns.Add(new Column(22, true, false, (string)null, ColumnType.DeleteButton));

            /////////////////////
            this.isListElement = this.Property.Parent != null && this.Property.Parent.ChildResolver is IOrderedCollectionResolver;
            var isList       = !this.isListElement;
            var listProperty = isList ? this.Property : this.Property.Parent;
            this.baseMemberProperty     = listProperty.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
            this.globalSelectedProperty = this.baseMemberProperty.Context.GetGlobal("selectedIndex" + this.baseMemberProperty.GetHashCode(), (InspectorProperty)null);
            selectedPropertyList        = new List<InspectorProperty>();
            if(isList)
            {
                var parentType = this.baseMemberProperty.ParentValues[0].GetType();
                this.selectedIndexSetter = EmitUtilities.CreateWeakInstanceMethodCaller<List<int>>(parentType.GetMethod(this.Attribute.SetSelectedMethod, Flags.AllMembers));
            }
            //////////////////////
        }

        /// <summary>Draws the property layout.</summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var t = Event.current.type;
            if(this.drawAsList)
            {
                if(GUILayout.Button("Draw as table"))
                    this.drawAsList = false;
                this.CallNextDrawer(label);
            }
            else
            {
                this.picker = ObjectPicker.GetObjectPicker((object)this, this.resolver.ElementType);
                this.paging.Update(this.resolver.MaxCollectionLength);
                this.currPage.Value         = this.paging.CurrentPage;
                this.isPagingExpanded.Value = this.paging.IsExpanded;
                Rect rect = SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyPadding);
                if(!this.Attribute.HideToolbar)
                    this.DrawToolbar(label);
                if(this.Attribute.AlwaysExpanded)
                {
                    this.Property.State.Expanded = true;
                    this.DrawColumnHeaders();
                    this.DrawTable(label);
                }
                else
                {
                    if(SirenixEditorGUI.BeginFadeGroup((object)this, this.Property.State.Expanded) && this.Property.Children.Count > 0)
                    {
                        this.DrawColumnHeaders();
                        this.DrawTable(label);
                        ////
                    }
                    SirenixEditorGUI.EndFadeGroup();
                }


                SirenixEditorGUI.EndIndentedVertical();
                if(Event.current.type == EventType.Repaint)
                {
                    --rect.yMin;
                    rect.height -= 3f;
                    SirenixEditorGUI.DrawBorders(rect, 1);
                }
                this.DropZone(rect);
                this.HandleObjectPickerEvents();
                if(Event.current.type != EventType.Repaint)
                    return;
                this.isFirstFrame = false;
            }
        }

        private void DrawSelectRect(GUIContent label)
        {
            var t   = Event.current.type;
            var key = Event.current.keyCode;
            if(t == EventType.Layout)
            {
                this.CallNextDrawer(label);
                return;
            }

            if(key == KeyCode.LeftControl && t == EventType.KeyDown)
            {
                isPressCtrl = true;
            }
            else if(key == KeyCode.LeftControl && t == EventType.KeyUp)
            {
                isPressCtrl = false;
            }

            if(key == KeyCode.LeftShift && t == EventType.KeyDown)
            {
                isPressShift = true;

                // Debug.Log("KeyDown Shift");
            }
            else if(key == KeyCode.LeftShift && t == EventType.KeyUp)
            {
                isPressShift = false;

                // Debug.Log("Keyup Shift");
            }

            if(isPressCtrl && key == KeyCode.A)
            {
                for(int rowIndexFrom = 0; rowIndexFrom < this.Property.Children.Count; ++rowIndexFrom)
                {
                    var target = this.Property.Children[rowIndexFrom];
                    selectedPropertyList.Add(target);
                }
                Select(selectedPropertyList);
            }

            // else
            // isPressCtrl = false;


            for(int rowIndexFrom = this.table.RowIndexFrom; rowIndexFrom < this.table.RowIndexTo; ++rowIndexFrom)
            {
                var rect = this.table.GetRowRect(rowIndexFrom);

                this.Property.Children[rowIndexFrom].Update();

                // var rect       = GUIHelper.GetCurrentLayoutRect();
                // var isSelected = this.globalSelectedProperty.Value == this.Property.Children[rowIndexFrom];
                var isSelected = selectedPropertyList.Contains(this.Property.Children[rowIndexFrom]);
                if(t == EventType.Repaint && isSelected)
                {
                    EditorGUI.DrawRect(rect, selectedColor);
                }

                if(t == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && !isPressCtrl && !isPressShift)
                {
                    // this.globalSelectedProperty.Value = this.Property.Children[rowIndexFrom];

                    var target = this.Property.Children[rowIndexFrom];
                    if(selectedPropertyList.Contains(target))
                    {
                        selectedPropertyList.Clear();
                    }
                    else
                    {
                        selectedPropertyList.Clear();
                        selectedPropertyList.Add(target);
                    }
                    Select(selectedPropertyList);
                }
                else
                {
                    if(t == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && isPressCtrl)
                    {
                        Debug.Log("Press");
                        var target = this.Property.Children[rowIndexFrom];
                        if(selectedPropertyList.Contains(this.Property.Children[rowIndexFrom]))
                        {
                            selectedPropertyList.Remove(target);
                        }
                        else
                            selectedPropertyList.Add(target);
                        Select(selectedPropertyList);
                    }

                    if(t == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && isPressShift)
                    {
                        if(selectedPropertyList.Count == 1)
                        {
                            if(rowIndexFrom > selectedPropertyList[0].Index)
                            {
                                for(var i = selectedPropertyList[0].Index; i < rowIndexFrom + 1; i++)
                                {
                                    var target = this.Property.Children[i];

                                    if(!selectedPropertyList.Contains(target))
                                    {
                                        selectedPropertyList.Add(target);
                                    }
                                }
                            }
                            else if(rowIndexFrom < selectedPropertyList[0].Index)
                            {
                                for(var i = rowIndexFrom; i < selectedPropertyList[0].Index; i++)
                                {
                                    var target = this.Property.Children[i];

                                    if(selectedPropertyList.Contains(target))
                                    {
                                        selectedPropertyList.Remove(target);
                                    }
                                    else
                                        selectedPropertyList.Add(target);
                                }
                            }

                            Select(selectedPropertyList);
                        }
                    }
                }
            }
        }

        private void Select(List<InspectorProperty> index)
        {
            GUIHelper.RequestRepaint();
            this.Property.Tree.DelayAction(() =>
            {
                var selector = index.Select(c => c.Index).ToList();
                for(int i = 0; i < this.baseMemberProperty.ParentValues.Count; i++)
                {
                    this.selectedIndexSetter(this.baseMemberProperty.ParentValues[i], selector);
                }
            });
        }

        private void OnChildValueChanged(int index)
        {
            IPropertyValueEntry valueEntry = this.Property.Children[index].ValueEntry;
            if(valueEntry == null || !typeof(ScriptableObject).IsAssignableFrom(valueEntry.TypeOfValue))
                return;
            for(int index1 = 0; index1 < valueEntry.ValueCount; ++index1)
            {
                Object weakValue = valueEntry.WeakValues[index1] as Object;
                if((bool)weakValue)
                    EditorUtility.SetDirty(weakValue);
            }
        }

        private void DropZone(Rect rect)
        {
            if(this.isReadOnly)
                return;
            EventType type = Event.current.type;
            switch(type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if(!rect.Contains(Event.current.mousePosition))
                        break;
                    Object[] objectArray = (Object[])null;
                    if(((IEnumerable<Object>)DragAndDrop.objectReferences).Any<Object>((Func<Object, bool>)(n =>
                           n != (Object)null && this.resolver.ElementType.IsAssignableFrom(n.GetType()))))
                        objectArray = ((IEnumerable<Object>)DragAndDrop.objectReferences)
                           .Where<Object>((Func<Object, bool>)(x => x != (Object)null && this.resolver.ElementType.IsAssignableFrom(x.GetType())))
                           .Reverse<Object>().ToArray<Object>();
                    else if(this.resolver.ElementType.InheritsFrom(typeof(Component)))
                        objectArray = (Object[])DragAndDrop.objectReferences.OfType<GameObject>()
                           .Select<GameObject, Component>((Func<GameObject, Component>)(x => x.GetComponent(this.resolver.ElementType)))
                           .Where<Component>((Func<Component, bool>)(x => (Object)x != (Object)null)).Reverse<Component>().ToArray<Component>();
                    else if(this.resolver.ElementType.InheritsFrom(typeof(Sprite))
                         && ((IEnumerable<Object>)DragAndDrop.objectReferences).Any<Object>((Func<Object, bool>)(n => n is Texture2D && AssetDatabase.Contains(n))))
                        objectArray = (Object[])DragAndDrop.objectReferences.OfType<Texture2D>()
                           .Select<Texture2D, Sprite>((Func<Texture2D, Sprite>)(x => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath((Object)x))))
                           .Where<Sprite>((Func<Sprite, bool>)(x => (Object)x != (Object)null)).Reverse<Sprite>().ToArray<Sprite>();
                    if(objectArray == null || (uint)objectArray.Length <= 0U)
                        break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    if(type != EventType.DragPerform)
                        break;
                    DragAndDrop.AcceptDrag();
                    foreach(Object @object in objectArray)
                    {
                        object[] values = new object[this.Property.ParentValues.Count];
                        for(int index = 0; index < values.Length; ++index)
                            values[index] = (object)@object;
                        this.resolver.QueueAdd(values);
                    }
                    break;
            }
        }

        private void AddColumns(int rowIndexFrom, int rowIndexTo)
        {
            if(Event.current.type != EventType.Layout)
                return;
            for(int index1 = rowIndexFrom; index1 < rowIndexTo; ++index1)
            {
                int               num    = 0;
                InspectorProperty child1 = this.Property.Children[index1];
                for(int index2 = 0; index2 < child1.Children.Count; ++index2)
                {
                    InspectorProperty child2 = child1.Children[index2];
                    if(this.seenColumnNames.Add(child2.Name))
                    {
                        if(this.GetColumnAttribute<HideInTablesAttribute>(child2) != null)
                        {
                            ++num;
                        }
                        else
                        {
                            bool                      preserveWidth   = false;
                            bool                      resizable       = true;
                            bool                      flag            = true;
                            int                       minWidth        = this.Attribute.DefaultMinColumnWidth;
                            TableColumnWidthAttribute columnAttribute = this.GetColumnAttribute<TableColumnWidthAttribute>(child2);
                            if(columnAttribute != null)
                            {
                                preserveWidth = !columnAttribute.Resizable;
                                resizable     = columnAttribute.Resizable;
                                minWidth      = columnAttribute.Width;
                                flag          = false;
                            }
                            Column column =
                                new Column(minWidth, preserveWidth, resizable, child2.Name, ColumnType.Property)
                                {
                                    NiceName = child2.NiceName
                                };
                            column.NiceNameLabelWidth = (int)SirenixGUIStyles.Label.CalcSize(new GUIContent(column.NiceName)).x;
                            column.PreferWide         = flag;
                            this.columns.Insert(Math.Min(index2 + this.colOffset - num, this.columns.Count), column);
                            GUIHelper.RequestRepaint(3);
                        }
                    }
                }
            }
        }

        private void DrawToolbar(GUIContent label)
        {
            Rect rect1 = GUILayoutUtility.GetRect(0.0f, 22f);
            bool flag  = Event.current.type == EventType.Repaint;
            if(flag)
                SirenixGUIStyles.ToolbarBackground.Draw(rect1, GUIContent.none, 0);
            if(!this.isReadOnly)
            {
                Rect rect2 = rect1.AlignRight(23f);
                --rect2.width;
                rect1.xMax = rect2.xMin;
                if(GUI.Button(rect2, GUIContent.none, SirenixGUIStyles.ToolbarButton))
                    this.picker.ShowObjectPicker((object)null, this.Property.GetAttribute<AssetsOnlyAttribute>() == null && !typeof(ScriptableObject).IsAssignableFrom(this.resolver.ElementType),
                        rect1,
                        !this.Property.ValueEntry.SerializationBackend.SupportsPolymorphism);
                EditorIcons.Plus.Draw(rect2, 16f);
            }
            if(!this.isReadOnly)
            {
                Rect rect2 = rect1.AlignRight(23f);
                rect1.xMax = rect2.xMin;
                if(GUI.Button(rect2, GUIContent.none, SirenixGUIStyles.ToolbarButton))
                    this.drawAsList = !this.drawAsList;
                EditorIcons.HamburgerMenu.Draw(rect2, 13f);
            }
            this.paging.DrawToolbarPagingButtons(ref rect1, this.Property.State.Expanded, true);
            if(label == null)
                label = GUIHelper.TempContent("");
            Rect rect3 = rect1;
            rect3.x      += 5f;
            rect3.y      += 3f;
            rect3.height =  16f;
            if(this.Property.Children.Count > 0)
            {
                GUIHelper.PushHierarchyMode(false);
                if(this.Attribute.AlwaysExpanded)
                    GUI.Label(rect3, label);
                else
                    this.Property.State.Expanded = SirenixEditorGUI.Foldout(rect3, this.Property.State.Expanded, label);
                GUIHelper.PushHierarchyMode(true);
            }
            else
            {
                if(!flag)
                    return;
                GUI.Label(rect3, label);
            }
        }

        private void DrawColumnHeaders()
        {
            if(this.Property.Children.Count == 0)
                return;
            this.columnHeaderRect = GUILayoutUtility.GetRect(0.0f, 21f);
            ++this.columnHeaderRect.height;
            --this.columnHeaderRect.y;
            if(Event.current.type == EventType.Repaint)
            {
                SirenixEditorGUI.DrawBorders(this.columnHeaderRect, 1);
                EditorGUI.DrawRect(this.columnHeaderRect, SirenixGUIStyles.ColumnTitleBg);
            }
            this.columnHeaderRect.width -= this.columnHeaderRect.width - this.table.ContentRect.width;
            GUITableUtilities.ResizeColumns<Column>(this.columnHeaderRect, (IList<Column>)this.columns);
            if(Event.current.type != EventType.Repaint)
                return;
            GUITableUtilities.DrawColumnHeaderSeperators<Column>(this.columnHeaderRect, (IList<Column>)this.columns,
                SirenixGUIStyles.BorderColor);
            Rect columnHeaderRect = this.columnHeaderRect;
            for(int index = 0; index < this.columns.Count; ++index)
            {
                Column column = this.columns[index];
                if((double)columnHeaderRect.x > (double)this.columnHeaderRect.xMax)
                    break;
                columnHeaderRect.width = column.ColWidth;
                columnHeaderRect.xMax  = Mathf.Min(this.columnHeaderRect.xMax, columnHeaderRect.xMax);
                if(column.NiceName != null)
                    GUI.Label(columnHeaderRect, column.NiceName, SirenixGUIStyles.LabelCentered);
                columnHeaderRect.x += column.ColWidth;
            }
        }

        private void DrawTable(GUIContent label)
        {
            GUIHelper.PushHierarchyMode(false);
            this.table.DrawScrollView = this.Attribute.DrawScrollView && (this.paging.IsExpanded || !this.paging.IsEnabled);
            this.table.ScrollPos      = this.scrollPos.Value;
            this.table.BeginTable(this.paging.EndIndex - this.paging.StartIndex);
            this.AddColumns(this.table.RowIndexFrom, this.table.RowIndexTo);
            this.DrawListItemBackGrounds();
            this.DrawSelectRect(label);
            float num = 0.0f;
            for(int index = 0; index < this.columns.Count; ++index)
            {
                Column column = this.columns[index];
                int    width  = (int)column.ColWidth;
                if(this.isFirstFrame && column.PreferWide)
                    width = 200;
                this.table.BeginColumn((int)num, width);
                GUIHelper.PushLabelWidth((float)width * 0.3f);
                num += column.ColWidth;
                for(int rowIndexFrom = this.table.RowIndexFrom; rowIndexFrom < this.table.RowIndexTo; ++rowIndexFrom)
                {
                    this.table.BeginCell(rowIndexFrom);
                    this.DrawCell(column, rowIndexFrom);
                    this.table.EndCell(rowIndexFrom);
                }
                GUIHelper.PopLabelWidth();
                this.table.EndColumn();
            }
            this.DrawRightClickContextMenuAreas();
            this.table.EndTable();
            this.scrollPos.Value = this.table.ScrollPos;
            this.DrawColumnSeperators();
            GUIHelper.PopHierarchyMode();
            if(this.columns.Count <= 0 || this.columns[0].ColumnType != ColumnType.Index)
                return;
            this.columns[0].ColWidth = (float)this.indexLabelWidth;
            this.columns[0].MinWidth = (float)this.indexLabelWidth;
        }

        private void DrawColumnSeperators()
        {
            if(Event.current.type != EventType.Repaint)
                return;
            Color borderColor = SirenixGUIStyles.BorderColor;
            borderColor.a *= 0.4f;
            GUITableUtilities.DrawColumnHeaderSeperators<Column>(this.table.OuterRect, (IList<Column>)this.columns, borderColor);
        }

        private void DrawListItemBackGrounds()
        {
            if(Event.current.type != EventType.Repaint)
                return;
            for(int rowIndexFrom = this.table.RowIndexFrom; rowIndexFrom < this.table.RowIndexTo; ++rowIndexFrom)
            {
                Color color = new Color();
                EditorGUI.DrawRect(this.table.GetRowRect(rowIndexFrom), rowIndexFrom % 2 == 0 ? SirenixGUIStyles.ListItemColorEven : SirenixGUIStyles.ListItemColorOdd);
            }
        }

        private void DrawRightClickContextMenuAreas()
        {
            for(int rowIndexFrom = this.table.RowIndexFrom; rowIndexFrom < this.table.RowIndexTo; ++rowIndexFrom)
            {
                Rect rowRect = this.table.GetRowRect(rowIndexFrom);
                this.Property.Children[rowIndexFrom].Update();
                PropertyContextMenuDrawer.AddRightClickArea(this.Property.Children[rowIndexFrom], rowRect);
            }
        }

        private void DrawCell(Column col, int rowIndex)
        {
            rowIndex += this.paging.StartIndex;
            if(col.ColumnType == ColumnType.Index)
            {
                Rect rect = GUILayoutUtility.GetRect(0.0f, 16f);
                rect.xMin  += 5f;
                rect.width -= 2f;
                if(Event.current.type != EventType.Repaint)
                    return;
                this.indexLabel.text = rowIndex.ToString();
                GUI.Label(rect, this.indexLabel, SirenixGUIStyles.Label);
                this.indexLabelWidth = Mathf.Max(this.indexLabelWidth, (int)SirenixGUIStyles.Label.CalcSize(this.indexLabel).x + 15);
            }
            else if(col.ColumnType == ColumnType.DeleteButton)
            {
                if(!SirenixEditorGUI.IconButton(GUILayoutUtility.GetRect(20f, 20f).AlignCenter(16f), EditorIcons.X))
                    return;
                this.resolver.QueueRemoveAt(rowIndex);
            }
            else
            {
                if(col.ColumnType != ColumnType.Property)
                    throw new NotImplementedException(col.ColumnType.ToString());
                this.Property.Children[rowIndex].Children[col.Name]?.Draw((GUIContent)null);
            }
        }

        private void HandleObjectPickerEvents()
        {
            if(!this.picker.IsReadyToClaim || Event.current.type != EventType.Repaint)
                return;
            object   obj    = this.picker.ClaimObject();
            object[] values = new object[this.Property.Tree.WeakTargets.Count];
            values[0] = obj;
            for(int index = 1; index < values.Length; ++index)
                values[index] = SerializationUtility.CreateCopy(obj);
            this.resolver.QueueAdd(values);
        }

        private IEnumerable<InspectorProperty> EnumerateGroupMembers(InspectorProperty groupProperty)
        {
            for(int i = 0; i < groupProperty.Children.Count; ++i)
            {
                if(groupProperty.Children[i].Info.PropertyType != PropertyType.Group)
                {
                    yield return groupProperty.Children[i];
                }
                else
                {
                    foreach(InspectorProperty enumerateGroupMember in this.EnumerateGroupMembers(groupProperty.Children[i]))
                        yield return enumerateGroupMember;
                }
            }
        }

        private T GetColumnAttribute<T>(InspectorProperty col) where T : Attribute => col.Info.PropertyType != PropertyType.Group
            ? col.GetAttribute<T>()
            : this.EnumerateGroupMembers(col).Select<InspectorProperty, T>((Func<InspectorProperty, T>)(c => c.GetAttribute<T>())).FirstOrDefault<T>((Func<T, bool>)(c => (object)c != null));

        private enum ColumnType
        {

            Property,
            Index,
            DeleteButton,

        }

        private class Column : IResizableColumn
        {

            public string     Name;
            public float      ColWidth;
            public float      MinWidth;
            public bool       Preserve;
            public bool       Resizable;
            public string     NiceName;
            public int        NiceNameLabelWidth;
            public ColumnType ColumnType;
            public bool       PreferWide;

            public Column(int        minWidth,
                          bool       preserveWidth,
                          bool       resizable,
                          string     name,
                          ColumnType colType
            )
            {
                this.MinWidth   = (float)minWidth;
                this.ColWidth   = (float)minWidth;
                this.Preserve   = preserveWidth;
                this.Name       = name;
                this.ColumnType = colType;
                this.Resizable  = resizable;
            }

            float IResizableColumn.ColWidth
            {
                get => this.ColWidth;
                set => this.ColWidth = value;
            }

            float IResizableColumn.MinWidth => this.MinWidth;

            bool IResizableColumn.PreserveWidth => this.Preserve;

            bool IResizableColumn.Resizable => this.Resizable;

        }

    }
}