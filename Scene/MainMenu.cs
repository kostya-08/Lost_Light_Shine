using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		CreateUI();
	}
	
	private void CreateUI()
	{
		// Фон
		var background = new ColorRect();
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		background.Color = new Color(0.05f, 0.05f, 0.1f);
		AddChild(background);
		
		// Заголовок
		var title = new Label();
		title.Text = "LOST LIGHT SHINE";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 56);
		title.Position = new Vector2(0, 60);
		title.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		title.Modulate = new Color(1, 0.8f, 0);
		AddChild(title);
		
		// Подзаголовок
		var subtitle = new Label();
		subtitle.Text = "Найди свой путь во тьме...";
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		subtitle.AddThemeFontSizeOverride("font_size", 24);
		subtitle.Position = new Vector2(0, 130);
		subtitle.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		subtitle.Modulate = new Color(0.7f, 0.7f, 0.7f);
		AddChild(subtitle);
		
		// Контейнер кнопок
		var buttonContainer = new VBoxContainer();
		buttonContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
		buttonContainer.Position = new Vector2(-150, -80);
		buttonContainer.Size = new Vector2(300, 200);
		buttonContainer.AddThemeConstantOverride("separation", 25);
		AddChild(buttonContainer);
		
		// Кнопка "Новая игра" - увеличенная
		var startBtn = new Button();
		startBtn.Text = "▶ НОВАЯ ИГРА";
		startBtn.CustomMinimumSize = new Vector2(300, 70);
		startBtn.AddThemeFontSizeOverride("font_size", 28);
		startBtn.Pressed += OnStartPressed;
		
		// Стилизация кнопки
		var startStyle = new StyleBoxFlat();
		startStyle.BgColor = new Color(0.2f, 0.2f, 0.3f);
		startStyle.SetCornerRadiusAll(12);
		startStyle.BorderWidthLeft = 2;
		startStyle.BorderWidthRight = 2;
		startStyle.BorderWidthTop = 2;
		startStyle.BorderWidthBottom = 2;
		startStyle.BorderColor = new Color(1, 0.8f, 0);
		startBtn.AddThemeStyleboxOverride("normal", startStyle);
		
		var startHover = new StyleBoxFlat();
		startHover.BgColor = new Color(0.3f, 0.3f, 0.4f);
		startHover.SetCornerRadiusAll(12);
		startBtn.AddThemeStyleboxOverride("hover", startHover);
		
		buttonContainer.AddChild(startBtn);
		
		// Кнопка "Выход" - увеличенная
		var exitBtn = new Button();
		exitBtn.Text = "✖ ВЫХОД";
		exitBtn.CustomMinimumSize = new Vector2(300, 70);
		exitBtn.AddThemeFontSizeOverride("font_size", 28);
		exitBtn.Pressed += OnExitPressed;
		
		var exitStyle = new StyleBoxFlat();
		exitStyle.BgColor = new Color(0.2f, 0.2f, 0.3f);
		exitStyle.SetCornerRadiusAll(12);
		exitStyle.BorderWidthLeft = 2;
		exitStyle.BorderWidthRight = 2;
		exitStyle.BorderWidthTop = 2;
		exitStyle.BorderWidthBottom = 2;
		exitStyle.BorderColor = new Color(1, 0.8f, 0);
		exitBtn.AddThemeStyleboxOverride("normal", exitStyle);
		
		var exitHover = new StyleBoxFlat();
		exitHover.BgColor = new Color(0.3f, 0.3f, 0.4f);
		exitHover.SetCornerRadiusAll(12);
		exitBtn.AddThemeStyleboxOverride("hover", exitHover);
		
		buttonContainer.AddChild(exitBtn);
		
		// Панель с подсказками по управлению
		var controlsPanel = new Panel();
		controlsPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		controlsPanel.Position = new Vector2(0, -120);
		controlsPanel.Size = new Vector2(400, 100);
		controlsPanel.Position = new Vector2((float)GetViewport().GetVisibleRect().Size.X / 2 - 200, -120);
		
		var panelStyle = new StyleBoxFlat();
		panelStyle.BgColor = new Color(0, 0, 0, 0.7f);
		panelStyle.SetCornerRadiusAll(10);
		controlsPanel.AddThemeStyleboxOverride("panel", panelStyle);
		
		// Контейнер для подсказок
		var controlsContainer = new HBoxContainer();
		controlsContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		controlsContainer.AddThemeConstantOverride("separation", 30);
		controlsContainer.Position = new Vector2(20, 15);
		controlsPanel.AddChild(controlsContainer);
		
		// Подсказка: Движение
		var moveBox = CreateControlBox("WASD / Стрелки", "Перемещение");
		controlsContainer.AddChild(moveBox);
		
		// Подсказка: Бег
		var runBox = CreateControlBox("Shift", "Ускорение (бег)");
		controlsContainer.AddChild(runBox);
		
		// Подсказка: Фонарик
		var flashlightBox = CreateControlBox("F", "Использовать батарейку");
		controlsContainer.AddChild(flashlightBox);
		
		// Подсказка: Взаимодействие
		var interactBox = CreateControlBox("E", "Взаимодействие (сундук/ключ)");
		controlsContainer.AddChild(interactBox);
		
		AddChild(controlsPanel);
		
		// Версия игры
		var version = new Label();
		version.Text = "v1.0";
		version.Position = new Vector2(10, -30);
		version.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		version.Modulate = new Color(0.5f, 0.5f, 0.5f);
		AddChild(version);
	}
	
	private VBoxContainer CreateControlBox(string key, string description)
	{
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 5);
		box.Size = new Vector2(120, 70);
		
		var keyLabel = new Label();
		keyLabel.Text = key;
		keyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		keyLabel.AddThemeFontSizeOverride("font_size", 18);
		keyLabel.Modulate = new Color(1, 0.8f, 0);
		
		var descLabel = new Label();
		descLabel.Text = description;
		descLabel.HorizontalAlignment = HorizontalAlignment.Center;
		descLabel.AddThemeFontSizeOverride("font_size", 12);
		descLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
		
		box.AddChild(keyLabel);
		box.AddChild(descLabel);
		
		return box;
	}
	
	private void OnStartPressed()
	{
		GD.Print("Запуск игры...");
		
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.5f);
		tween.Finished += () =>
		{
			GetTree().ChangeSceneToFile("res://Scene/Level.tscn");
		};
	}
	
	private void OnExitPressed()
	{
		GD.Print("Выход из игры...");
		GetTree().Quit();
	}
}
