#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine.Controls.Visuals.Triggers;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Bindings;

namespace SkinEngine.Controls.Visuals
{
  public enum VisibilityEnum
  {
    Visible = 0,
    Hidden = 1,
    Collapsed = 2,
  }
  public class UIElement : Visual, IBindingCollection
  {
    Property _nameProperty;
    Property _keyProperty;
    Property _focusableProperty;
    Property _isFocusScopeProperty;
    Property _hasFocusProperty;
    Property _acutalPositionProperty;
    Property _positionProperty;
    Property _dockProperty;
    Property _marginProperty;
    Property _triggerProperty;
    Property _renderTransformProperty;
    Property _renderTransformOriginProperty;
    Property _layoutTransformProperty;
    Property _visibilityProperty;
    Property _isEnabledProperty;
    Property _rowProperty;
    Property _columnProperty;
    Property _rowSpanProperty;
    Property _columnSpanProperty;
    Property _isItemsHostProperty;
    Property _contextProperty;
    Property _opacityMask;
    protected Size _desiredSize;
    protected Size _availableSize;
    protected System.Drawing.Rectangle _finalRect;
    bool _isArrangeValid;
    ResourceDictionary _resources;
    List<Timeline> _runningAnimations;
    BindingCollection _bindings;
    bool _isLayoutInvalid = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIElement"/> class.
    /// </summary>
    public UIElement()
    {
      Init();

    }
    public UIElement(UIElement el)
      : base((Visual)el)
    {
      Init();
      Name = el.Name;
      Key = el.Key;
      Focusable = el.Focusable;
      IsFocusScope = el.IsFocusScope;
      HasFocusProperty.SetValue(el.HasFocus);
      ActualPosition = el.ActualPosition;
      Position = el.Position;
      Dock = el.Dock;
      Margin = el.Margin;
      Visibility = el.Visibility;
      IsEnabled = el.IsEnabled;
      Row = el.Row;
      Column = el.Column;
      RowSpan = el.RowSpan;
      ColumnSpan = el.ColumnSpan;
      IsItemsHost = el.IsItemsHost;
      Context = el.Context;
      if (OpacityMask != null)
        OpacityMask = (SkinEngine.Controls.Brushes.Brush)el.OpacityMask.Clone();

      foreach (Binding binding in el._bindings)
      {
        _bindings.Add((Binding)binding.Clone());
      }

      if (el.LayoutTransform != null)
        LayoutTransform = (Transform)el.LayoutTransform.Clone();
      if (el.RenderTransform != null)
        RenderTransform = (Transform)el.RenderTransform.Clone();

      RenderTransformOrigin = el.RenderTransformOrigin;
      IDictionaryEnumerator enumer = el.Resources.GetEnumerator();
      while (enumer.MoveNext())
      {
        ICloneable clone = enumer.Value as ICloneable;
        if (clone != null)
        {
          Resources[enumer.Key] = clone.Clone();
        }
        else
        {
          Resources[enumer.Key] = enumer.Value;
          Trace.WriteLine(String.Format("resource type:{0} is not clonable", enumer.Value));
        }
      }

      foreach (Trigger t in el.Triggers)
      {
        Triggers.Add((Trigger)t.Clone());
      }
    }
    void Init()
    {
      _bindings = new BindingCollection();
      _runningAnimations = new List<Timeline>();
      _nameProperty = new Property("");
      _keyProperty = new Property("");
      _focusableProperty = new Property(false);
      _isFocusScopeProperty = new Property(true);
      _hasFocusProperty = new Property(false);
      _acutalPositionProperty = new Property(new Vector3(0, 0, 1));
      _positionProperty = new Property(new Vector3(0, 0, 1));
      _dockProperty = new Property(Dock.Center);
      _marginProperty = new Property(new Vector4(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new Property(new TriggerCollection());
      _renderTransformProperty = new Property(null);
      _layoutTransformProperty = new Property(null);
      _renderTransformOriginProperty = new Property(new Vector2(0, 0));
      _visibilityProperty = new Property(VisibilityEnum.Visible);
      _isEnabledProperty = new Property(true);
      _isItemsHostProperty = new Property(false);
      _contextProperty = new Property(null);

      _rowProperty = new Property(1);
      _columnProperty = new Property(1);
      _rowSpanProperty = new Property(1);
      _columnSpanProperty = new Property(1);
      _opacityMask = new Property(null);

      _positionProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _dockProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _marginProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _visibilityProperty.Attach(new PropertyChangedHandler(OnVisibilityPropertyChanged));
    }

    void OnVisibilityPropertyChanged(Property property)
    {
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
      }
    }

    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      Invalidate();
    }

    /// <summary>
    /// Gets or sets the resources.
    /// </summary>
    /// <value>The resources.</value>
    public ResourceDictionary Resources
    {
      get
      {
        return _resources;
      }
    }

    /// <summary>
    /// Gets or sets the context property.
    /// </summary>
    /// <value>The context property.</value>
    public Property ContextProperty
    {
      get
      {
        return _contextProperty;
      }
      set
      {
        _contextProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the context.
    /// </summary>
    /// <value>The context.</value>
    public object Context
    {
      get
      {
        return _contextProperty.GetValue();
      }
      set
      {
        _contextProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the opacity mask property.
    /// </summary>
    /// <value>The opacity mask property.</value>
    public Property OpacityMaskProperty
    {
      get
      {
        return _opacityMask;
      }
      set
      {
        _opacityMask = value;
      }
    }

    /// <summary>
    /// Gets or sets the opacity mask.
    /// </summary>
    /// <value>The opacity mask.</value>
    public SkinEngine.Controls.Brushes.Brush OpacityMask
    {
      get
      {
        return _opacityMask.GetValue() as SkinEngine.Controls.Brushes.Brush;
      }
      set
      {
        _opacityMask.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is items host property.
    /// </summary>
    /// <value>The is items host property.</value>
    public Property IsItemsHostProperty
    {
      get
      {
        return _isItemsHostProperty;
      }
      set
      {
        _isItemsHostProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this element hosts items or not
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is items host; otherwise, <c>false</c>.
    /// </value>
    public bool IsItemsHost
    {
      get
      {
        return (bool)_isItemsHostProperty.GetValue();
      }
      set
      {
        _isItemsHostProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the row property.
    /// </summary>
    /// <value>The row property.</value>
    public Property RowProperty
    {
      get
      {
        return _rowProperty;
      }
      set
      {
        _rowProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the row.
    /// </summary>
    /// <value>The row.</value>
    public int Row
    {
      get
      {
        return (int)_rowProperty.GetValue();
      }
      set
      {
        _rowProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the row property.
    /// </summary>
    /// <value>The row property.</value>
    public Property RowSpanProperty
    {
      get
      {
        return _rowSpanProperty;
      }
      set
      {
        _rowSpanProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the row.
    /// </summary>
    /// <value>The row.</value>
    public int RowSpan
    {
      get
      {
        return (int)_rowSpanProperty.GetValue();
      }
      set
      {
        _rowSpanProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the column property.
    /// </summary>
    /// <value>The column property.</value>
    public Property ColumnProperty
    {
      get
      {
        return _columnProperty;
      }
      set
      {
        _columnProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the column.
    /// </summary>
    /// <value>The column.</value>
    public int Column
    {
      get
      {
        return (int)_columnProperty.GetValue();
      }
      set
      {
        _columnProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the column span property.
    /// </summary>
    /// <value>The column span property.</value>
    public Property ColumnSpanProperty
    {
      get
      {
        return _columnSpanProperty;
      }
      set
      {
        _columnSpanProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the column span.
    /// </summary>
    /// <value>The column span.</value>
    public int ColumnSpan
    {
      get
      {
        return (int)_columnSpanProperty.GetValue();
      }
      set
      {
        _columnSpanProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is enabled property.
    /// </summary>
    /// <value>The is enabled property.</value>
    public Property IsEnabledProperty
    {
      get
      {
        return _isEnabledProperty;
      }
      set
      {
        _isEnabledProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is enabled.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsEnabled
    {
      get
      {
        return (bool)_isEnabledProperty.GetValue();
      }
      set
      {
        _isEnabledProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the visibility property.
    /// </summary>
    /// <value>The visibility property.</value>
    public Property VisibilityProperty
    {
      get
      {
        return _visibilityProperty;
      }
      set
      {
        _visibilityProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the visibility.
    /// </summary>
    /// <value>The visibility.</value>
    public VisibilityEnum Visibility
    {
      get
      {
        return (VisibilityEnum)_visibilityProperty.GetValue();
      }
      set
      {
        _visibilityProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the triggers property.
    /// </summary>
    /// <value>The triggers property.</value>
    public Property TriggersProperty
    {
      get
      {
        return _triggerProperty;
      }
      set
      {
        _triggerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the triggers.
    /// </summary>
    /// <value>The triggers.</value>
    public TriggerCollection Triggers
    {
      get
      {
        return (TriggerCollection)_triggerProperty.GetValue();
      }
    }
    /// <summary>
    /// Gets or sets the actual position property.
    /// </summary>
    /// <value>The actual position property.</value>
    public Property ActualPositionProperty
    {
      get
      {
        return _acutalPositionProperty;
      }
      set
      {
        _acutalPositionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the actual position.
    /// </summary>
    /// <value>The actual position.</value>
    public Vector3 ActualPosition
    {
      get
      {
        return (Vector3)_acutalPositionProperty.GetValue();
      }
      set
      {
        _acutalPositionProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the name property.
    /// </summary>
    /// <value>The name property.</value>
    public Property NameProperty
    {
      get
      {
        return _nameProperty;
      }
      set
      {
        _nameProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _nameProperty.GetValue() as string;
      }
      set
      {
        _nameProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the element has focus property.
    /// </summary>
    /// <value>The has focus property.</value>
    public Property HasFocusProperty
    {
      get
      {
        return _hasFocusProperty;
      }
      set
      {
        _hasFocusProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this uielement has focus.
    /// </summary>
    /// <value><c>true</c> if this uielement has focus; otherwise, <c>false</c>.</value>
    public virtual bool HasFocus
    {
      get
      {
        return (bool)_hasFocusProperty.GetValue();
      }
      set
      {
        _hasFocusProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the is focusable property.
    /// </summary>
    /// <value>The is focusable property.</value>
    public Property FocusableProperty
    {
      get
      {
        return _focusableProperty;
      }
      set
      {
        _focusableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the is focusable.
    /// </summary>
    /// <value>The is focusable.</value>
    public bool Focusable
    {
      get
      {
        return (bool)_focusableProperty.GetValue();
      }
      set
      {
        _focusableProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is focus scope property.
    /// </summary>
    /// <value>The is focus scope property.</value>
    public Property IsFocusScopeProperty
    {
      get
      {
        return _isFocusScopeProperty;
      }
      set
      {
        _isFocusScopeProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets a value indicating whether this instance is focus scope.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is focus scope; otherwise, <c>false</c>.
    /// </value>
    public bool IsFocusScope
    {
      get
      {
        return (bool)_isFocusScopeProperty.GetValue();
      }
      set
      {
        _isFocusScopeProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the position property.
    /// </summary>
    /// <value>The position property.</value>
    public Property PositionProperty
    {
      get
      {
        return _positionProperty;
      }
      set
      {
        _positionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    /// <value>The position.</value>
    public Vector3 Position
    {
      get
      {
        return (Vector3)_positionProperty.GetValue();
      }
      set
      {
        _positionProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the left position.
    /// </summary>
    /// <value>The left position.</value>
    public double Left
    {
      get
      {
        Vector3 v = (Vector3)_positionProperty.GetValue();
        return v.X;
      }
      set
      {
        Vector3 pos = Position;
        pos.X = (float)value;
        Position = pos;
      }
    }
    /// <summary>
    /// Gets or sets the top position.
    /// </summary>
    /// <value>The top position.</value>
    public double Top
    {
      get
      {
        Vector3 v = (Vector3)_positionProperty.GetValue();
        return v.Y;
      }
      set
      {
        Vector3 pos = Position;
        pos.Y = (float)value;
        Position = pos;
      }
    }

    /// <summary>
    /// Gets or sets the dock property.
    /// </summary>
    /// <value>The dock property.</value>
    public Property DockProperty
    {
      get
      {
        return _dockProperty;
      }
      set
      {
        _dockProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the dock.
    /// </summary>
    /// <value>The dock.</value>
    public Dock Dock
    {
      get
      {
        return (Dock)_dockProperty.GetValue();
      }
      set
      {
        _dockProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets a value indicating whether this instance is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is visible; otherwise, <c>false</c>.
    /// </value>
    public bool IsVisible
    {
      get
      {
        return (this.Visibility == VisibilityEnum.Visible);
      }
    }


    /// <summary>
    /// Gets or sets the margin property.
    /// </summary>
    /// <value>The margin property.</value>
    public Property MarginProperty
    {
      get
      {
        return _marginProperty;
      }
      set
      {
        _marginProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the margin.
    /// </summary>
    /// <value>The margin.</value>
    public Vector4 Margin
    {
      get
      {
        return (Vector4)_marginProperty.GetValue();
      }
      set
      {
        _marginProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the layout transform property.
    /// </summary>
    /// <value>The layout transform property.</value>
    public Property LayoutTransformProperty
    {
      get
      {
        return _layoutTransformProperty;
      }
      set
      {
        _layoutTransformProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the layout transform.
    /// </summary>
    /// <value>The layout transform.</value>
    public Transform LayoutTransform
    {
      get
      {
        return _layoutTransformProperty.GetValue() as Transform;
      }
      set
      {
        _layoutTransformProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the render transform property.
    /// </summary>
    /// <value>The render transform property.</value>
    public Property RenderTransformProperty
    {
      get
      {
        return _renderTransformProperty;
      }
      set
      {
        _renderTransformProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the render transform.
    /// </summary>
    /// <value>The render transform.</value>
    public Transform RenderTransform
    {
      get
      {
        return _renderTransformProperty.GetValue() as Transform;
      }
      set
      {
        _renderTransformProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the render transform origin property.
    /// </summary>
    /// <value>The render transform origin property.</value>
    public Property RenderTransformOriginProperty
    {
      get
      {
        return _renderTransformOriginProperty;
      }
      set
      {
        _renderTransformOriginProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the render transform origin.
    /// </summary>
    /// <value>The render transform origin.</value>
    public Vector2 RenderTransformOrigin
    {
      get
      {
        return (Vector2)_renderTransformOriginProperty.GetValue();
      }
      set
      {
        _renderTransformOriginProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this UIElement has been layout
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this UIElement is arrange valid; otherwise, <c>false</c>.
    /// </value>
    public bool IsArrangeValid
    {
      get
      {
        return _isArrangeValid;
      }
      set
      {
        _isArrangeValid = value;
      }
    }

    /// <summary>
    /// Gets desired size
    /// </summary>
    /// <value>The desired size.</value>
    public Size DesiredSize
    {
      get
      {
        return _desiredSize;
      }
    }
    /// <summary>
    /// Gets the size for brush.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public virtual void GetSizeForBrush(out double width, out double height)
    {
      width = 0.0;
      height = 0.0;
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements. </param>
    /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
    public virtual void Measure(Size availableSize)
    {
      _availableSize = new Size(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element 
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public virtual void Arrange(System.Drawing.Rectangle finalRect)
    {
      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
    }

    /// <summary>
    /// Invalidates the layout of this uielement.
    /// If dimensions change, it will invalidate the parent visual so 
    /// the parent will re-layout itself and its children
    /// </summary>
    public virtual void Invalidate()
    {
      if (!IsArrangeValid) return;
      _isLayoutInvalid = true;
    }

    public void UpdateLayout()
    {
      if (false == _isLayoutInvalid) return;
      Trace.WriteLine("UpdateLayout :" + this.Name + "  " + this.GetType());
      _isLayoutInvalid = false;
      if (_availableSize.Width > 0 && _availableSize.Height > 0)
      {
        System.Drawing.Size sizeOld = new Size(_desiredSize.Width, _desiredSize.Height);
        System.Drawing.Size availsizeOld = new Size(_availableSize.Width, _availableSize.Height);
        Measure(_availableSize);
        _availableSize = availsizeOld;
        if (_desiredSize == sizeOld)
        {
          Arrange(_finalRect);
          return;
        }
      }
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
        VisualParent.UpdateLayout();
      }
      else
      {
        FrameworkElement element = this as FrameworkElement;
        if (element == null)
        {
          Measure(new Size((int)SkinContext.Width, (int)SkinContext.Height));
          Arrange(new System.Drawing.Rectangle(0, 0, (int)SkinContext.Width, (int)SkinContext.Height));
        }
        else
        {
          int w = (int)element.Width;
          int h = (int)element.Height;
          if (w == 0) w = (int)SkinContext.Width;
          if (h == 0) h = (int)SkinContext.Height;
          Measure(new Size(w, h));
          Arrange(new System.Drawing.Rectangle((int)element.Position.X, (int)element.Position.Y, w, h));
        }
      }
    }
    /// <summary>
    /// Finds the resource with the given keyname
    /// </summary>
    /// <param name="resourceKey">The key name.</param>
    /// <returns>resource, or null if not found.</returns>
    public object FindResource(string resourceKey)
    {
      if (Resources.Contains(resourceKey))
      {
        return Resources[resourceKey];
      }
      if (VisualParent != null)
      {
        return VisualParent.FindResource(resourceKey);
      }
      return null;
    }

    public void InitializeTriggers()
    {
      foreach (Trigger trigger in Triggers)
      {
        trigger.Setup(this);
      }
    }
    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public virtual void FireEvent(string eventName)
    {
      foreach (Trigger trigger in Triggers)
      {
        EventTrigger eventTrig = trigger as EventTrigger;
        if (eventTrig != null)
        {
          if (eventTrig.RoutedEvent == eventName)
          {
            if (eventTrig.Storyboard != null)
            {
              StartStoryboard(eventTrig.Storyboard);
            }
          }
        }
      }
    }

    public void StartStoryboard(Timeline board)
    {
      lock (_runningAnimations)
      {
        if (!_runningAnimations.Contains(board))
        {
          _runningAnimations.Add(board);
          board.VisualParent = this;
          board.Start(SkinContext.TimePassed);
        }
      }
    }

    public void StopStoryboard(Timeline board)
    {
      lock (_runningAnimations)
      {
        board.Stop();
        _runningAnimations.Remove(board);
      }
    }

    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public virtual void Animate()
    {
      if (_runningAnimations.Count == 0) return;
      List<Timeline> stoppedAnimations = new List<Timeline>();
      lock (_runningAnimations)
      {
        foreach (Timeline line in _runningAnimations)
        {
          line.Animate(SkinContext.TimePassed);
          if (line.IsStopped)
            stoppedAnimations.Add(line);
        }
        foreach (Timeline line in stoppedAnimations)
        {
          line.Stop();
          _runningAnimations.Remove(line);
        }
      }
    }
    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public virtual void OnMouseMove(float x, float y)
    {
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public virtual void OnKeyPressed(ref Key key)
    {
    }

    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public virtual UIElement FindElement(string name)
    {
      if (Name == name)
        return this;
      return null;
    }
    /// <summary>
    /// Finds the element of type t.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <returns></returns>
    public virtual UIElement FindElementType(Type t)
    {
      if (this.GetType() == t) return this;
      return null;
    }
    /// <summary>
    /// Finds the the element which is a ItemsHost
    /// </summary>
    /// <returns></returns>
    public virtual UIElement FindItemsHost()
    {
      if (IsItemsHost) return this;
      return null;
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public virtual UIElement FindFocusedItem()
    {
      if (HasFocus) return this;
      return null;
    }


    #region IBindingCollection Members

    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }


    public virtual void InitializeBindings()
    {
      if (_bindings.Count == 0) return;
      foreach (Binding binding in _bindings)
      {
        binding.Initialize(this);
      }
    }
    #endregion
  }
}
