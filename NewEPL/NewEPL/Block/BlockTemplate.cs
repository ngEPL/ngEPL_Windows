﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace NewEPL {
    public class BlockTemplate : UserControl, ICloneable {

        /// <summary>
        /// ZIndex지정을 위한 변수
        /// </summary>
        public MainWindow Main;

        public double X {
            get {
                return Canvas.GetLeft(this);
            }
            set {
                Canvas.SetLeft(this, value);
            }
        }

        public double Y {
            get {
                return Canvas.GetTop(this);
            }
            set {
                Canvas.SetTop(this, value);
            }
        }

        /// <summary>
        /// 부모 블록과 상대적인 거리 차이
        /// </summary>
        public double DifX = 0;
        public double DifY = 0;

        public int ZIndex {
            get {
                return Canvas.GetZIndex(this);
            }
            set {
                Canvas.SetZIndex(this, value);
            }
        }

        /// <summary>
        /// 블록을 드래그 해서 ZIndex를 변경할 때 임시로 현재 ZIndex를 담아두는 변수
        /// </summary>
        public int TempZIndex = 0;

        /// <summary>
        /// 부모 블록
        /// </summary>
        public BlockTemplate BlockParent = null;

        /// <summary>
        /// 부모 연결자
        /// </summary>
        public ISplicer SplicerParent = null;

        public bool IsResized = false;

        /// <summary>
        ///  고유 코드
        /// </summary>
        public static readonly DependencyProperty IdProperty;
        public string Id { 
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        // 나중에 이름 바꾸기
        public static readonly DependencyProperty BlockTypeProperty;
        public int BlockType {
            get { return (int)GetValue(BlockTypeProperty); }
            set { SetValue(BlockTypeProperty, value); }
        }

        public object LValue = null;
        public object RValue = null;

        static BlockTemplate() {
            IdProperty = DependencyProperty.Register("Id", typeof(string), typeof(BlockTemplate), new UIPropertyMetadata(null));
            BlockTypeProperty = DependencyProperty.Register("BlockType", typeof(int), typeof(BlockTemplate), new UIPropertyMetadata(null));
        }

        /// <summary>
        /// 블록 생성할 때 블록 리스트에 있는 선택된 블록을 복사하는 함수.
        /// </summary>
        /// <param name="b">선택된 블록</param>
        /// <returns>복제된 블록</returns>

        public object Clone() {
            var ret = (BlockTemplate)Activator.CreateInstance(GetType());
            var thumb = ret.GetThumb();
            var canvas = ret.GetSplicerCanvas();

            ret.Width = Width;

            thumb.ApplyTemplate();
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragDelta += Thumb_DragDelta;
            thumb.DragCompleted += Thumb_DragCompleted;

            var image = (Image9)thumb.Template.FindName("image", thumb);
            foreach (var i in canvas.Children) {
                var splicer = (Splicer)i;
                Canvas.SetLeft(splicer, (splicer.X + (image.Patch.GetImmutableWidth(0) + image.Patch.GetStrectedWidth(image.DefaultWidth)) * 0));
                Canvas.SetTop(splicer, (splicer.Y + (image.Patch.GetImmutableHeight(splicer.YStack) + image.Patch.GetStrectedHeight(image.DefaultHeight)) * splicer.YStack));

                if (Double.IsNaN(splicer.Width)) {
                    splicer.Width = ret.Width + splicer.RelativeWidth;
                }
            }

            //ret.ZIndex = 0;

            return ret;
        }

        public void MoveBlocks(double x, double y) {
            X = x;
            Y = y;

            foreach (var i in GetSplicers(1, true)) {
                if (i.GetType() == typeof(Splicer)) {
                    foreach (var j in ((Splicer)i).BlockChildren) {
                        j.MoveBlocks(j.DifX + x, j.DifY + y);
                    }
                } else if (i.GetType() == typeof(ExtendedTextBox)) {
                    foreach (var j in ((ExtendedTextBox)i).BlockChildren) {
                        if (i == j) continue;
                        j.MoveBlocks(j.DifX + x, j.DifY + y);
                    }
                }
            }
        }

        public void SetZIndex(int zindex) {
            ZIndex = zindex;
            TempZIndex = zindex;

            foreach (var i in GetSplicers(1, true)) {
                if (i.GetType() == typeof(Splicer)) {
                    foreach (var j in ((Splicer)i).BlockChildren) {
                        j.SetZIndex(ZIndex + 1);
                    }
                } else if (i.GetType() == typeof(ExtendedTextBox)) {
                    foreach (var j in ((ExtendedTextBox)i).BlockChildren) {
                        j.SetZIndex(ZIndex + 1);
                    }
                }
            }
        }

        public void SetTempZIndex(int zindex) {
            TempZIndex = ZIndex;
            ZIndex = zindex;

            foreach (var i in GetSplicers(1, true)) {
                if (i.GetType() == typeof(Splicer))     {
                    foreach (var j in ((Splicer)i).BlockChildren) {
                        j.SetTempZIndex(ZIndex + 1);
                    }
                } else if (i.GetType() == typeof(ExtendedTextBox)) {
                    foreach (var j in ((ExtendedTextBox)i).BlockChildren) {
                        j.SetTempZIndex(ZIndex + 1);
                    }
                }
            }
        }

        public void AddChild(BlockTemplate child, ISplicer splicer) {
            var spl = (Splicer)splicer;
            (child.Content as BlockTemplate).BlockParent = this;
            (child.Content as BlockTemplate).SplicerParent = spl;
            spl.BlockChildren.Add(child);
            child.MoveBlocks(X + Canvas.GetLeft(spl), Y + Canvas.GetTop(spl));
            child.DifX = -(this.X - child.X);
            child.DifY = -(this.Y - child.Y);
            SetZIndex(ZIndex + 1);
        }

        public Thumb GetThumb() {
            if (GetType() == typeof(BlockTemplate))
                return (Thumb)VisualTreeHelper.GetChild((DependencyObject)(Content as BlockTemplate).Content, 0);
            return (Thumb)VisualTreeHelper.GetChild((DependencyObject)Content, 0);
        }

        public Canvas GetSplicerCanvas() {
            if (GetType() == typeof(BlockTemplate))
                return (Canvas)VisualTreeHelper.GetChild((DependencyObject)(Content as BlockTemplate).Content, 1);
            return (Canvas)VisualTreeHelper.GetChild((DependencyObject)Content, 1);
        }

        public StackPanel GetContentPanel() {
            if (GetType() == typeof(BlockTemplate))
                return (StackPanel)VisualTreeHelper.GetChild((DependencyObject)(Content as BlockTemplate).Content, 2);
            return (StackPanel)VisualTreeHelper.GetChild((DependencyObject)Content, 2);
        }

        public Splicer GetSplicer(int idx) {
            var canvas = (Canvas)VisualTreeHelper.GetChild((DependencyObject)(Content as BlockTemplate).Content, 1);
            return (Splicer)VisualTreeHelper.GetChild(canvas.Children[idx] as DependencyObject, 0);
        }

        /// <summary>
        /// Splicer를 가져옴
        /// </summary>
        /// <param name="what">0일 때 Type=False만 가져옴. 1일 때 Type=True만 가져옴. -1일 때 모두 가져옴.</param>
        /// <param name="content">true일 때 콘텐츠패널 Splicer도 가져옴.</param>
        /// <returns></returns>
        public List<ISplicer> GetSplicers(int what, bool content) {
            List<ISplicer> ret = new List<ISplicer>();

            var canvas = GetSplicerCanvas();
            foreach(var i in canvas.Children) {
                var splicer = (Splicer)i;
                if (what == -1) ret.Add(splicer);
                else if (splicer.Type == Convert.ToBoolean(what)) ret.Add(splicer);
            }

            if (content && what == 1) {
                var contentPanel = GetContentPanel();
                foreach (var i in contentPanel.Children) {
                    if (i.GetType() != typeof(ExtendedTextBox)) continue;
                    ret.Add((ExtendedTextBox)i);
                }
            }

            return ret;
        }

        protected void RemoveChild(BlockTemplate child) {
            foreach(var i in GetSplicers(1, false)) {
                var splicer = (Splicer)i;
                foreach(var j in splicer.BlockChildren) {
                    if(j.Equals(child)) {
                        splicer.BlockChildren.Remove(j);
                        return;
                    }
                }
            }
        }

        protected void UpdateSplicer(Image9 image, int width, int height) {
            var canvas = GetSplicerCanvas();
            foreach (var i in canvas.Children) {
                var splicer = (Splicer)i;
                Canvas.SetLeft(splicer, (splicer.X + (image.Patch.GetImmutableWidth(0) + image.Patch.GetStrectedWidth(0)) * 0));
                Canvas.SetTop(splicer, (splicer.Y + (image.Patch.GetImmutableHeight(splicer.YStack) + image.Patch.GetStrectedHeight(height)) * splicer.YStack));

                if (Double.IsNaN(splicer.Width)) {
                    splicer.Width = Width + splicer.RelativeWidth; /// 0.8 -> 배율
                }
            }
        }

        public Rect GetBoundingBox(ISplicer splicer) {
            var spl = (Splicer)splicer;
            return new Rect(X + Canvas.GetLeft(spl), Y + Canvas.GetTop(spl), spl.Width, spl.Height);
        }

        /// 블록마다 길이 구하는 방식이 다를 수 있음 (IF블록 같은 경우 안쪽 자식 블록은 계산하지 않고 밑 블록만 계산에 넣음)
        public virtual double GetTotalHeight() {
            double ret = ActualHeight;

            foreach(var i in GetSplicers(1, false)) {
                var splicer = (Splicer)i;

                foreach(var j in splicer.BlockChildren) {
                    ret += (j.Content as BlockTemplate).GetTotalHeight() - 10;
                }
            }

            return ret;
        }

        public virtual void SetWidth(double width) {
            var thumb = GetThumb();
            var image = (Image9)thumb.Template.FindName("image", thumb);

            image.Source = image.Patch.GetPatchedImage(new List<int>() { (int)width, 0, 0 } , new List<int>() { 0, 0, 0 }, image.Color);
            UpdateSplicer(image, (int)image.Patch.Width, (int)image.Patch.Height);

            (Content as BlockTemplate).Width = image.Patch.Width;
        }

        public virtual void SetHeight(double height) {
            var thumb = GetThumb();
            var image = (Image9)thumb.Template.FindName("image", thumb);

            image.Source = image.Patch.GetPatchedImage(new List<int>() { 0, 0, 0 }, new List<int>() { (int)height, 0, 0 }, image.Color);
            UpdateSplicer(image, (int)image.Patch.Width, (int)image.Patch.Height);
        }

        public virtual void IncreaseHeight(Splicer what, double height, int lv) {
            if (BlockParent != null) {
                (BlockParent.Content as BlockTemplate).IncreaseHeight(what, height, lv + 1);
            }
        }

        public virtual void DecreaseHeight(Splicer what, double height, int lv) {
            if (BlockParent != null) {
                (BlockParent.Content as BlockTemplate).DecreaseHeight(what, height, lv + 1);
            }
        }

        public virtual void AddBlock(BlockTemplate child, ISplicer splicer) {

        }

        protected virtual void CheckDragAction(BlockTemplate b) {

            bool escaper = false;
            bool isCollision = false;

            var splicers = b.GetSplicers(0, false);

            foreach (var i in b.Main.BlockCanvas.Children) {
                if (escaper) break;
                if (i.GetType() != typeof(BlockTemplate)) continue;
                if (i.Equals(b)) continue;
                if (splicers.Count <= 0) continue;

                var other = (BlockTemplate)i;
                foreach (var j in other.GetSplicers(1, false)) {
                    var thisSplicer = (Splicer)splicers[0];
                    var otherSplicer = (Splicer)j;

                    if (otherSplicer.BlockChildren.Count > 0) continue;

                    if (b.GetBoundingBox(thisSplicer).IntersectsWith(other.GetBoundingBox(otherSplicer))) {
                        Canvas.SetLeft(b.Main.TestPreview, other.X + Canvas.GetLeft(otherSplicer));
                        Canvas.SetTop(b.Main.TestPreview, other.Y + Canvas.GetTop(otherSplicer));
                        b.Main.TestPreview.Visibility = Visibility.Visible;
                        
                        b.CollideSplicer = otherSplicer;
                        isCollision = true;

                        escaper = true;
                        break;
                    }
                }
            }

            if (!isCollision) {
                if (b.IsResized) {
                    (((b.CollideSplicer.Parent as Canvas).Parent as Grid).Parent as BlockTemplate).DecreaseHeight(b.CollideSplicer, (b.Content as BlockTemplate).GetTotalHeight(), 0);
                    b.IsResized = false;
                }
            }
        }
        
        ///  이름 바꾸기
        protected virtual void CheckDropAction(BlockTemplate b) {
            bool escaper = false;

            foreach (var i in b.Main.BlockCanvas.Children) {
                if (escaper) break;
                if (i.GetType() != typeof(BlockTemplate)) continue;
                if (i.Equals(b)) continue;

                var splicers = b.GetSplicers(0, false);
                if (splicers.Count <= 0) continue;

                var other = (BlockTemplate)i;
                if (((BlockTemplate)other.Content).BlockType == 1) continue;

                foreach (var j in other.GetSplicers(1, false)) {
                    var thisSplicer = (Splicer)splicers[0];
                    var otherSplicer = (Splicer)j;

                    if (otherSplicer.BlockChildren.Count > 0) continue;

                    if (b.GetBoundingBox(thisSplicer).IntersectsWith(other.GetBoundingBox(otherSplicer))) {
                        other.AddChild(b, otherSplicer);
                        b.Main.TestPreview.Visibility = Visibility.Hidden;

                        if (!b.IsResized) {
                            (((otherSplicer.Parent as Canvas).Parent as Grid).Parent as BlockTemplate).IncreaseHeight(otherSplicer, (b.Content as BlockTemplate).GetTotalHeight(), 0);
                            b.IsResized = true;
                        }

                        escaper = true;
                        break;
                    }
                }
            }
        }

        public Border CollideBorder = null;
        public Splicer CollideSplicer = null;

        private static void Thumb_DragStarted(object sender, DragStartedEventArgs e) {
            var b = (((sender as Thumb).Parent as Grid).Parent as BlockTemplate).Parent as BlockTemplate;

            b.SetTempZIndex(99999999);

            b.Main = (MainWindow)Window.GetWindow(b);
        }

        /// 이미지 늘어나는 기능을 따로 빼기.
        private static void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            var b = (((sender as Thumb).Parent as Grid).Parent as BlockTemplate).Parent as BlockTemplate;

            b.MoveBlocks(b.X + e.HorizontalChange, b.Y + e.VerticalChange);

            //var canvas = (b.Parent as Canvas);
            //b.X = Math.Min(0, canvas.ActualWidth - b.ActualWidth);
            //b.Y = Math.Min(0, canvas.ActualHeight - b.ActualHeight);
            //b.MoveBlocks(b.X, b.Y);

            if ((b.Content as BlockTemplate).BlockParent != null) {
                (b.Content as BlockTemplate).BlockParent.RemoveChild(b);
                (b.Content as BlockTemplate).BlockParent = null;
            }

            (b.Content as BlockTemplate).CheckDragAction(b);
        }

        /// 추가해야하는 기능. 겹치는 두 블록 그룹에 자식을 추가할 때 마우스 포인터와 충돌해 있는 블록 그룹에 자식이 붙어야 함.
        private static void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) {
            var b = (((sender as Thumb).Parent as Grid).Parent as BlockTemplate).Parent as BlockTemplate;

            (b.Content as BlockTemplate).CheckDropAction(b);

            b.SetTempZIndex(b.TempZIndex);
        }
    }
}
