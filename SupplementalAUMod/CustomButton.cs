using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AUMod {
public class CustomButton {
    public static List<CustomButton> buttons = new List<CustomButton>();
    public KillButton killButton;
    public Vector3 PositionOffset;
    public float MaxTimer = float.MaxValue;
    public float Timer = 0f;
    private Action OnClick;
    private Action OnMeetingEnds;
    private Func<bool> HasButton;
    private Func<bool> CouldUse;
    private Action OnEffectEnds;
    public bool HasEffect;
    public bool isEffectActive = false;
    private bool showButtonText = false;
    public float EffectDuration;
    public Sprite Sprite;
    private HudManager hudManager;
    private bool mirror;
    private KeyCode? hotkey;

    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool HasEffect, float EffectDuration, Action OnEffectEnds, bool mirror = false)
    {
        this.hudManager = hudManager;
        this.OnClick = OnClick;
        this.HasButton = HasButton;
        this.CouldUse = CouldUse;
        this.PositionOffset = PositionOffset;
        this.OnMeetingEnds = OnMeetingEnds;
        this.HasEffect = HasEffect;
        this.EffectDuration = EffectDuration;
        this.OnEffectEnds = OnEffectEnds;
        this.Sprite = Sprite;
        this.mirror = mirror;
        this.hotkey = hotkey;
        Timer = 16.2f;
        buttons.Add(this);
        killButton = UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.transform);
        this.showButtonText = killButton.graphic.sprite == Sprite;
        PassiveButton button = killButton.GetComponent<PassiveButton>();
        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClickEvent);

        setActive(false);
    }

    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool mirror = false)
        : this(OnClick, HasButton, CouldUse, OnMeetingEnds, Sprite, PositionOffset, hudManager, hotkey, false, 0f, () => {}, mirror)
    {
    }

    void onClickEvent()
    {
        if (this.Timer < 0f && HasButton() && CouldUse()) {
            this.OnClick();

            killButton.SetDisabled();

            if (this.HasEffect && !this.isEffectActive) {
                this.Timer = this.EffectDuration;
                killButton.SetEnabled();
                this.isEffectActive = true;
            }
        }
    }

    public static void HudUpdate()
    {
        buttons.RemoveAll(item => item.killButton == null);

        for (int i = 0; i < buttons.Count; i++) {
            try {
                buttons[i].Update();
            } catch (NullReferenceException) {
                System.Console.WriteLine("[WARNING] NullReferenceException from HudUpdate().HasButton(), if theres only one warning its fine");
            }
        }
    }

    public static void MeetingEndedUpdate()
    {
        buttons.RemoveAll(item => item.killButton == null);
        for (int i = 0; i < buttons.Count; i++) {
            try {
                buttons[i].OnMeetingEnds();
                buttons[i].Update();
            } catch (NullReferenceException) {
                System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
            }
        }
    }

    public static void ResetAllCooldowns()
    {
        for (int i = 0; i < buttons.Count; i++) {
            try {
                buttons[i].Timer = buttons[i].MaxTimer;
                buttons[i].Update();
            } catch (NullReferenceException) {
                System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
            }
        }
    }

    public void setActive(bool isActive)
    {
        if (isActive)
            killButton.Show();
        else
            killButton.Hide();
    }

    private void Update()
    {
        if (PlayerControl.LocalPlayer.Data == null || MeetingHud.Instance || ExileController.Instance || !HasButton()) {
            setActive(false);
            return;
        }

        setActive(true);

        if (CouldUse()) {
            killButton.SetEnabled();
        } else {
            killButton.SetDisabled();
        }

        if (Timer >= 0) {
            if (HasEffect && isEffectActive)
                Timer -= Time.deltaTime;
            else if (!PlayerControl.LocalPlayer.inVent && PlayerControl.LocalPlayer.moveable)
                Timer -= Time.deltaTime;
        }

        // using
        if (Timer <= 0 && HasEffect && isEffectActive) {
            isEffectActive = false;
            killButton.SetEnabled();
            OnEffectEnds();
        }

        killButton.SetCoolDown(Timer, (HasEffect && isEffectActive) ? EffectDuration : MaxTimer);

        // Trigger OnClickEvent if the hotkey is being pressed down
        if (hotkey.HasValue && Input.GetKeyDown(hotkey.Value))
            onClickEvent();
    }
}
}
