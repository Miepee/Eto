using NUnit.Framework;
using System.Runtime.ExceptionServices;
namespace Eto.Test.UnitTests.Forms.Controls
{
	[TestFixture]
	public class ControlTests : TestBase
	{
		[TestCaseSource(nameof(GetControlTypes))]
		public void DefaultValuesShouldBeCorrect(IControlTypeInfo<Control> controlType)
		{
			TestProperties(f => controlType.CreateControl(),
						   c => c.Enabled,
						   c => c.ToolTip,
						   c => c.TabIndex
			);
		}

		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlShouldFireShownEvent(IControlTypeInfo<Control> controlType)
		{
			int shownCount = 0;
			int visualControlShownCount = 0;
			int expectedVisualShown = 0;
			Form(form =>
			{
				var ctl = controlType.CreateControl();

				// themed controls have visual controls!
				foreach (var visualControl in ctl.VisualControls)
				{
					expectedVisualShown++;
					visualControl.Shown += (sender, e) =>
					{
						visualControlShownCount++;
					};
				}
				ctl.Shown += (sender, e) =>
				{
					shownCount++;
					Application.Instance.AsyncInvoke(() =>
					{
						if (form.Loaded)
							form.Close();
					});
				};
				form.Content = TableLayout.AutoSized(ctl);
				Assert.That(shownCount, Is.EqualTo(0));
			});
			Assert.That(shownCount, Is.EqualTo(1));
			Assert.That(visualControlShownCount, Is.EqualTo(expectedVisualShown), "Visual controls didn't get Shown event triggered");
		}

		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlShouldFireShownEventWhenAddedDynamically(IControlTypeInfo<Control> controlType)
		{
			Exception exception = null;
			int shownCount = 0;
			Form(form =>
			{
				form.Size = new Size(200, 200);
				form.Shown += (sender, e) => Application.Instance.AsyncInvoke(() =>
				{
					try
					{
						var ctl = controlType.CreateControl();
						ctl.Shown += (sender2, e2) =>
						{
							shownCount++;
							Application.Instance.AsyncInvoke(() =>
							{
								if (form.Loaded)
									form.Close();
							});
						};
						form.Content = TableLayout.AutoSized(ctl);
					}
					catch (Exception ex)
					{
						exception = ex;
					}
				});
			});
			if (exception != null)
				ExceptionDispatchInfo.Capture(exception).Throw();

			Assert.That(shownCount, Is.EqualTo(1));
		}

		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlShouldFireShownEventWhenVisibleChanged(IControlTypeInfo<Control> controlType)
		{
			int shownCount = 0;
			int? initialShownCount = null;
			Form(form =>
			{
				var ctl = controlType.CreateControl();
				ctl.Shown += (sender2, e2) =>
				{
					shownCount++;
					Application.Instance.AsyncInvoke(() =>
					{
						if (form.Loaded)
							form.Close();
					});
				};
				ctl.Visible = false;
				Assert.That(shownCount, Is.EqualTo(0));
				form.Content = TableLayout.AutoSized(ctl);
				Assert.That(shownCount, Is.EqualTo(0));
				form.Shown += (sender, e) => Application.Instance.AsyncInvoke(() =>
				{
					initialShownCount = shownCount;
					ctl.Visible = true;
				});
			});

			Assert.That(initialShownCount, Is.EqualTo(0), "#1"); // should not be initially called
			Assert.That(shownCount, Is.EqualTo(1), "#2");
		}

		[ManualTest, Test]
		public void ControlsShouldHaveSaneDefaultWidths()
		{
			ManualForm(
				"Check to make sure the text/entry boxes have the correct widths and do not grow when entering text",
				form =>
				{
					var longText = "Some very long text that should not make the control grow larger than its default size";
					return new Scrollable
					{
						Content = new StackLayout
						{
							Items =
							{
								new Button { Text = "Button" },
								new Calendar(),
								new CheckBox { Text = "CheckBox" },
								new ColorPicker(),
								new ComboBox { Text = longText, Items = { "Item 1", "Item 2", "Item 3" } },
								new DateTimePicker(),
								new Drawable { Size = new Size(100, 20), BackgroundColor = Colors.Blue }, // not actually visible without a size
								new DropDown { Items = { "Item 1", "Item 2", "Item 3" } },
								new Expander { Header = "Hello", Content = new Label { Text = "Some content" } },
								new FilePicker { FilePath = "/some/path/that/is/long/which/should/not/make/it/too/big" },
								new GroupBox { Content = "Some content", Text = "Some text" },
								new LinkButton {  Text = "LinkButton"},
								new ListBox { Items = { "Item 1", "Item 2", "Item 3" } },
								new NumericStepper(),
								new PasswordBox(),
								new ProgressBar { Value = 50 },
								new RadioButton { Text = "RadioButton" },
								new RichTextArea { Text = longText },
								new SearchBox { Text = longText },
								new Slider { Value = 50 },
								new Spinner(),
								new Stepper(),
								new TabControl { Pages = { new TabPage { Text = "TabPage", Content = "Tab content" } } },
								new TextArea { Text = longText },
								new TextBox { Text = longText },
								new TextStepper { Text = longText },
						  		//new WebView()
					  		}
						}
					};
				});
		}

		public class ControlGCTest
		{
			public string Description { get; set; }
			public Type ControlType { get; set; }
			public Action<object> Test { get; set; }
			public override string ToString()
			{
				if (!string.IsNullOrEmpty(Description))
					return $"{ControlType}: {Description}";
				return ControlType.ToString();
			}

		}

		public static ControlGCTest GCTest<T>(Action<T> action) => new ControlGCTest { ControlType = typeof(T), Test = c => action((T)c) };

		public static ControlGCTest GCTest<T>(string description, Action<T> action)
		{
			var test = GCTest<T>(action);
			test.Description = description;
			return test;
		}

		public static IEnumerable<ControlGCTest> GetControlGCItems()
		{
			// simply create all control types and ensure they can be GC'd without hooking up anything.
			foreach (var type in GetAllControlTypes())
			{
				if (Platform.Instance.IsWpf || Platform.Instance.IsMac)
				{
					// wpf and macos has (known) problems GC'ing a Window right away, so let's not test it.
					if (typeof(Window).GetTypeInfo().IsAssignableFrom(type.Type))
						continue;
				}

				if (Platform.Instance.IsWpf || Platform.Instance.IsWinForms)
				{ 
					// SWF.WebBrowser can't be GC'd for some reason either.  Not an Eto problem.
					if (typeof(WebView).GetTypeInfo().IsAssignableFrom(type.Type))
						continue;
				}

				yield return new ControlGCTest { ControlType = type.Type };
			}

			// extra tests for things that have known to cause a control not to be GC'd

			yield return GCTest("With Step", (Stepper c) =>
			{
				c.Step += (sender, e) => { /* do something */ };
			});

			yield return GCTest("With ValueChanged", (NumericStepper c) =>
			{
				c.ValueChanged += (sender, e) => { /* do something */ };
			});

			yield return GCTest("With Step", (TextStepper c) =>
			{
				c.Step += (sender, e) => { /* do something */ };
			});

			yield return GCTest("With Panels", (Splitter c) =>
			{
				c.Panel1 = new Panel();
				c.Panel2 = new Panel();
			});
		}

		[TestCaseSource(nameof(GetControlGCItems))]
		public void ControlsShouldCollectWhenNotReferenced(ControlGCTest test)
		{
			WeakReference reference = null;
			Invoke(() =>
			{
				var obj = Activator.CreateInstance(test.ControlType);
				test.Test?.Invoke(obj);
				reference = new WeakReference(obj);
				obj = null;
			});
			Thread.Sleep(100);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.That(reference, Is.Not.Null);
			Assert.That(reference.Target, Is.Null);
			Assert.That(reference.IsAlive, Is.False);
		}

		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlsShouldReturnAFont(IControlTypeInfo<Control> info)
		{
			Invoke(() =>
			{
				var control = info.CreateControl();
				if (control is CommonControl commonControl)
				{
					Assert.That(commonControl.Font, Is.Not.Null);
				}
				else if (control is GroupBox groupBox)
				{
					Assert.That(groupBox.Font, Is.Not.Null);
				}
				else
				{
					Assert.Pass("Control does not have a font property");
				}
			});
		}

		[TestCaseSource(nameof(GetControlTypes)), ManualTest]
		public void ControlsShouldNotHaveIntrinsicPadding(IControlTypeInfo<Control> info)
		{
			ManualForm("Controls should be touching horizontally and vertically,\nwithout being clipped.", form =>
			{
				return new TableLayout
				{
					Rows =
					{
						new TableRow(new TableCell(info.CreatePopulatedControl(), true), new TableCell(info.CreatePopulatedControl(), true)),
						new TableRow(new Panel { Content = info.CreatePopulatedControl() }, info.CreatePopulatedControl()),
						new TableRow(info.CreatePopulatedControl(), new Drawable { Content = info.CreatePopulatedControl() }),
						null
					}
				};
			});
		}

		[Test, ManualTest]
		public void PointToScreenShouldWorkOnSecondaryScreen()
		{
			bool wasClicked = false;
			PointF? controlPoint = null;
			PointF? rountripPoint = null;
			Form childForm = null;
			try
			{
				ManualForm("The Form with the button should be above the text box exactly.\nClick the button to pass the test, close the window to fail.", form =>
				{
					var screens = Screen.Screens.ToArray();
					Assert.That(screens.Length, Is.GreaterThanOrEqualTo(2), "You must have a secondary monitor for this test");
					form.Location = Point.Round(screens[1].Bounds.Location) + new Size(50, 50);
					form.ClientSize = new Size(200, 200);

					var textBox = new TextBox { Text = "You shouldn't see this" };

					form.Shown += (sender, e) =>
					{
						controlPoint = PointF.Empty;
						var screenPoint = textBox.PointToScreen(PointF.Empty);
						rountripPoint = Point.Truncate(textBox.PointFromScreen(screenPoint));
						
						if (controlPoint != rountripPoint)
						{
							form.Close();
							return;
						}
						
						childForm = new Form
						{
							WindowStyle = WindowStyle.None,
							ShowInTaskbar = false,
							Maximizable = false,
							Resizable = false,
							BackgroundColor = Colors.Red,
							Topmost = true,
							Location = Point.Round(screenPoint),
							Size = textBox.Size
						};
						form.LocationChanged += (sender2, e2) =>
						{
							childForm.Location = Point.Round(textBox.PointToScreen(PointF.Empty));
							childForm.Size = textBox.Size;
						};
						var b = new Button { Text = "Click Me!" };
						b.Click += (sender2, e2) =>
						{
							wasClicked = true;
							childForm.Close();
							childForm = null;
							form.Close();
						};

						childForm.Content = new TableLayout { Rows = { b } };
						childForm.Show();
					};

					var layout = new DynamicLayout();
					layout.AddCentered(textBox);

					return layout;
				}, allowPass: false, allowFail: false);
			}
			finally
			{
				if (childForm != null)
					Application.Instance.Invoke(() => childForm.Close());
			}
			Assert.That(rountripPoint, Is.EqualTo(controlPoint), "Point could not round trip to screen then back");
			Assert.That(wasClicked, Is.True, "The test completed without clicking the button");
		}

		[TestCaseSource(nameof(GetControlTypes)), InvokeOnUI]
		public void ControlsShouldHavePreferredSize(IControlTypeInfo<Control> info)
		{
			var control = info.CreatePopulatedControl();
			var size = control.GetPreferredSize();
			Console.WriteLine($"PreferredSize for {info.Type}: {size}");
			Assert.That(size.Width, Is.GreaterThan(0), "#1.1 - Preferred width should be greater than zero");
			Assert.That(size.Height, Is.GreaterThan(0), "#1.2 - Preferred height should be greater than zero");
			var padding = new Padding(10);
			var container = new Panel { Content = control, Padding = padding };
			var containerSize = container.GetPreferredSize();
			Assert.That(containerSize.Width, Is.EqualTo(size.Width + padding.Horizontal).Within(0.1), "#2.1 - panel with padding should have correct width");
			Assert.That(containerSize.Height, Is.EqualTo(size.Height + padding.Vertical).Within(0.1), "#2.2 - panel with padding should have correct height");
		}

		[ManualTest]
		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlsShouldNotGetMouseOrFocusEventsWhenDisabled(IControlTypeInfo<Control> info)
		{
			ControlsShouldNotGetMouseOrFocusEventsWhenParentDisabled(info, false);
		}
		
		[ManualTest]
		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlsShouldNotGetMouseOrFocusEventsWhenParentDisabled(IControlTypeInfo<Control> info)
		{
			ControlsShouldNotGetMouseOrFocusEventsWhenParentDisabled(info, true);
		}

		public void ControlsShouldNotGetMouseOrFocusEventsWhenParentDisabled(IControlTypeInfo<Control> info, bool disableWithParent)
		{
			bool gotFocus = false;
			bool gotMouseDown = false;
			bool gotMouseUp = false;
			bool gotMouseEnter = false;
			bool gotMouseLeave = false;
			ManualForm("Click on the control, it should not get focus", form =>
			{
				var control = info.CreatePopulatedControl();
				if (!disableWithParent)
					control.Enabled = false;

				control.GotFocus += (sender, e) =>
				{
					Console.WriteLine("GotFocus");
					gotFocus = true;
				};
				control.LostFocus += (sender, e) =>
				{
					Console.WriteLine("LostFocus");
				};
				control.MouseDown += (sender, e) =>
				{
					Console.WriteLine("MouseDown");
					gotMouseDown = true;
				};
				control.MouseUp += (sender, e) =>
				{
					Console.WriteLine("MouseUp");
					gotMouseUp = true;
				};
				control.MouseEnter += (sender, e) =>
				{
					Console.WriteLine("MouseEnter");
					gotMouseEnter = true;
				};
				control.MouseLeave += (sender, e) =>
				{
					Console.WriteLine("MouseLeave");
					gotMouseLeave = true;
				};

				var panel = new Panel { Content = control };
				if (disableWithParent)
					panel.Enabled = false;
				return panel;
			});
			Assert.That(gotFocus, Is.False, "#1.1 - Control should not be able to get focus");
			Assert.That(gotMouseEnter, Is.False, "#1.2 - Got MouseEnter");
			Assert.That(gotMouseLeave, Is.False, "#1.3 - Got MouseLeave");
			Assert.That(gotMouseDown, Is.False, "#1.4 - Got MouseDown");
			Assert.That(gotMouseUp, Is.False, "#1.5 - Got MouseUp");
		}
		
		[ManualTest]
		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlShouldFireMouseLeaveIfEnteredThenDisabled(IControlTypeInfo<Control> info)
		{
			int mouseLeaveCalled = 0;
			int mouseEnterCalled = 0;
			bool mouseLeaveCalledBeforeMouseDown = false;
			bool mouseLeaveCalledAfterDisabled = false;
			int mouseDownCalled = 0;
			bool formClosing = false;
			bool mouseLeaveCalledAfterFormClosed = false;
			int enabledChanged = 0;
			bool enabledChangedFiredAfterMouseLeave = false;
			ManualForm("Click on the control", form =>
			{
				form.Closing += (sender, e) =>
				{
					formClosing = true;
				};

				var control = info.CreatePopulatedControl();
				control.MouseEnter += (sender, e) =>
				{
					mouseEnterCalled++;
				};
				control.MouseLeave += (sender, e) =>
				{
					mouseLeaveCalled++;
					mouseLeaveCalledAfterFormClosed |= formClosing;
					if (mouseDownCalled > 0)
						Application.Instance.AsyncInvoke(form.Close);
				};
				control.MouseDown += (sender, e) =>
				{
					mouseDownCalled++;
					mouseLeaveCalledBeforeMouseDown = mouseLeaveCalled > 0;
					control.Enabled = false;
					mouseLeaveCalledAfterDisabled = mouseLeaveCalled > 0;
					e.Handled = true;
				};
				control.EnabledChanged += (sender, e) =>
				{
					enabledChanged++;
					enabledChangedFiredAfterMouseLeave = mouseLeaveCalled > 0;

				};
				return control;
			});
			Assert.That(mouseEnterCalled, Is.EqualTo(1), "#1.1 - MouseEnter should be called exactly once");
			Assert.That(mouseLeaveCalled, Is.EqualTo(1), "#1.2 - MouseLeave should be called exactly once");
			Assert.That(mouseLeaveCalledBeforeMouseDown, Is.False, "#1.3 - MouseLeave should not have been called before MouseDown");
			Assert.That(mouseLeaveCalledAfterDisabled, Is.False, "#1.4 - MouseLeave should not be called during Enabled=false, but sometime after the MouseDown completes");
			Assert.That(mouseDownCalled, Is.EqualTo(1), "#1.5 - MouseDown should get called exactly once.  Did you click the control?");
			Assert.That(mouseLeaveCalledAfterFormClosed, Is.False, "#1.6 - MouseLeave should be called immediately when clicked, not when the form is closed");
			Assert.That(enabledChanged, Is.EqualTo(1), "#1.7 - EnabledChanged should be called exactly once");
			Assert.That(enabledChangedFiredAfterMouseLeave, Is.False, "#1.8 - MouseLeave should be fired after EnabledChanged event");
		}

		[ManualTest]
		[TestCaseSource(nameof(GetControlTypes))]
		public void ControlShouldFireMouseLeaveWhenUnloaded(IControlTypeInfo<Control> info)
		{
			int mouseLeaveCalled = 0;
			int mouseEnterCalled = 0;
			bool mouseLeaveCalledBeforeMouseDown = false;
			int mouseDownCalled = 0;
			bool formClosing = false;
			bool mouseLeaveCalledAfterFormClosed = false;
			ManualForm("Click on the control", form =>
			{
				form.Closing += (sender, e) =>
				{
					formClosing = true;
				};

				var control = info.CreatePopulatedControl();
				control.MouseEnter += (sender, e) =>
				{
					mouseEnterCalled++;
				};
				control.MouseLeave += (sender, e) =>
				{
					mouseLeaveCalled++;
					mouseLeaveCalledAfterFormClosed |= formClosing;
					if (mouseDownCalled > 0)
						Application.Instance.AsyncInvoke(form.Close);
				};
				control.MouseDown += (sender, e) =>
				{
					mouseDownCalled++;
					mouseLeaveCalledBeforeMouseDown = mouseLeaveCalled > 0;
					e.Handled = true;

					control.VisualParent.Remove(control);
				};
				return control;
			});
			Assert.That(mouseEnterCalled, Is.EqualTo(1), "#1.1 - MouseEnter should be called exactly once");
			Assert.That(mouseLeaveCalled, Is.EqualTo(1), "#1.2 - MouseLeave should be called exactly once");
			Assert.That(mouseLeaveCalledBeforeMouseDown, Is.False, "#1.3 - MouseLeave should not have been called before MouseDown");
			Assert.That(mouseDownCalled, Is.EqualTo(1), "#1.5 - MouseDown should get called exactly once.  Did you click the control?");
			Assert.That(mouseLeaveCalledAfterFormClosed, Is.False, "#1.6 - MouseLeave should be called immediately when clicked, not when the form is closed");
		}
	}
}