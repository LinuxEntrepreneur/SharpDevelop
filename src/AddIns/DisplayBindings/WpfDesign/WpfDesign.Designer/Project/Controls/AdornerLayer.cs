﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.WpfDesign.Adorners;

namespace ICSharpCode.WpfDesign.Designer.Controls
{
	/// <summary>
	/// A control that displays adorner panels.
	/// </summary>
	sealed class AdornerLayer : Panel
	{
		#region AdornerPanelCollection
		internal sealed class AdornerPanelCollection : ICollection<AdornerPanel>
		{
			readonly AdornerLayer _layer;
			
			public AdornerPanelCollection(AdornerLayer layer)
			{
				this._layer = layer;
			}
			
			public int Count {
				get { return _layer.Children.Count; }
			}
			
			public bool IsReadOnly {
				get { return false; }
			}
			
			public void Add(AdornerPanel item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				_layer.AddAdorner(item);
			}
			
			public void Clear()
			{
				_layer.ClearAdorners();
			}
			
			public bool Contains(AdornerPanel item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				return VisualTreeHelper.GetParent(item) == _layer;
			}
			
			public void CopyTo(AdornerPanel[] array, int arrayIndex)
			{
				Linq.ToArray(this).CopyTo(array, arrayIndex);
			}
			
			public bool Remove(AdornerPanel item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				return _layer.RemoveAdorner(item);
			}
			
			public IEnumerator<AdornerPanel> GetEnumerator()
			{
				foreach (AdornerPanel panel in _layer.Children) {
					yield return panel;
				}
			}
			
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
		}
		#endregion
		
		AdornerPanelCollection _adorners;
		readonly UIElement _designPanel;
		
		internal AdornerLayer(UIElement designPanel)
		{
			this._designPanel = designPanel;
			
			_adorners = new AdornerPanelCollection(this);
			ClearAdorners();
		}
		
		internal AdornerPanelCollection Adorners {
			get {
				return _adorners;
			}
		}
		
		sealed class AdornerInfo
		{
			internal readonly List<AdornerPanel> adorners = new List<AdornerPanel>();
		}
		
		// adorned element => AdornerInfo
		Dictionary<UIElement, AdornerInfo> _dict;
		
		void ClearAdorners()
		{
			this.Children.Clear();
			_dict = new Dictionary<UIElement, AdornerInfo>();
		}
		
		AdornerInfo GetAdornerInfo(UIElement adornedElement)
		{
			AdornerInfo info;
			if (!_dict.TryGetValue(adornedElement, out info)) {
				info = _dict[adornedElement] = new AdornerInfo();
			}
			return info;
		}
		
		AdornerInfo GetExistingAdornerInfo(UIElement adornedElement)
		{
			AdornerInfo info;
			_dict.TryGetValue(adornedElement, out info);
			return info;
		}
		
		void AddAdorner(AdornerPanel adornerPanel)
		{
			if (adornerPanel.AdornedElement == null)
				throw new DesignerException("adornerPanel.AdornedElement must be set");
			
			GetAdornerInfo(adornerPanel.AdornedElement).adorners.Add(adornerPanel);
			
			UIElementCollection children = this.Children;
			int i = 0;
			for (i = 0; i < children.Count; i++) {
				AdornerPanel p = (AdornerPanel)children[i];
				if (p.Order.CompareTo(adornerPanel.Order) > 0) {
					break;
				}
			}
			children.Insert(i, adornerPanel);
			
			this.InvalidateMeasure();
		}
		
		protected override Size MeasureOverride(Size availableSize)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			foreach (AdornerPanel adorner in this.Children) {
				adorner.Measure(infiniteSize);
			}
			return new Size(0, 0);
		}
		
		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (AdornerPanel adorner in this.Children) {
				adorner.Arrange(new Rect(new Point(0, 0), adorner.DesiredSize));
				adorner.RenderTransform = (Transform)adorner.AdornedElement.TransformToAncestor(_designPanel);
			}
			return finalSize;
		}
		
		bool RemoveAdorner(AdornerPanel adornerPanel)
		{
			if (adornerPanel.AdornedElement == null)
				return false;
			
			AdornerInfo info = GetExistingAdornerInfo(adornerPanel.AdornedElement);
			if (info == null)
				return false;
			
			if (info.adorners.Remove(adornerPanel)) {
				this.Children.Remove(adornerPanel);
				return true;
			} else {
				return false;
			}
		}
	}
}
