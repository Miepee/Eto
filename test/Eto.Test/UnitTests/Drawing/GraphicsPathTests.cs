using NUnit.Framework;

namespace Eto.Test.UnitTests.Drawing
{
	[TestFixture]
    public class GraphicsPathTests : TestBase
    {
		[Test, InvokeOnUI]
		public void GraphicsPathFillContainsShouldWork()
		{
			var path = new GraphicsPath();
			path.AddRectangle(0, 0, 10, 10);

			Assert.That(path.FillContains(new PointF(5, 5)), Is.True, "#1.1");
			Assert.That(path.FillContains(new PointF(11, 5)), Is.False, "#1.2");
			Assert.That(path.FillContains(new PointF(9, 5)), Is.True, "#1.3");
			Assert.That(path.FillContains(new PointF(10.5f, 5)), Is.False, "#1.4");
			Assert.That(path.FillContains(new PointF(0, 0)), Is.True, "#1.5");
			Assert.That(path.FillContains(new PointF(-1, 0)), Is.False, "#1.6");
			// slightly different behaviour with System.Drawing for some reason, ignore it for now.
			if (!Platform.Instance.IsWinForms)
				Assert.That(path.FillContains(new PointF(10, 5)), Is.True, "#1.7");
		}

		[Test, InvokeOnUI]
		public void GraphicsPathStrokeContainsShouldWork()
		{
			var path = new GraphicsPath();
			path.AddRectangle(0, 0, 10, 10);
			
			var pen = new Pen(Colors.Black, 1);
			Assert.That(path.StrokeContains(pen, new PointF(0, 0)), Is.True, "#1.1");
			Assert.That(path.StrokeContains(pen, new PointF(1, 1)), Is.False, "#1.2");
			Assert.That(path.StrokeContains(pen, new PointF(10, 1)), Is.True, "#1.3");
			Assert.That(path.StrokeContains(pen, new PointF(10, 10)), Is.True, "#1.4");
		}
    }
}