﻿using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
[RequireComponent(requiredComponent: typeof(Animator))]
public class MessagePanel : MonoBehaviour
{
    public Button PlayButton;
    public Button CheckButton;
    public Text CheckButtonText;

    private void Awake()
    {
        var animator = GetComponent<Animator>();
        Assert.IsNotNull(value: animator);

        Assert.IsNotNull(value: PlayButton);
        Assert.IsNotNull(value: CheckButton);
        Assert.IsNotNull(value: PlayButton.onClick);
        Assert.IsNotNull(value: CheckButton.onClick);

        PlayButton.onClick.AddListener(
            call: () => Patcher.Instance.OnStartAppRequested());

        CheckButton.onClick.AddListener(
            call: () => Patcher.Instance.OnUpdateAppRequested());

        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);

            Assert.IsNotNull(value: PlayButton);
            Assert.IsNotNull(value: CheckButton);
            Assert.IsNotNull(value: CheckButtonText);

            animator.SetBool(
                name: "IsOpened",
                value: state.Kind == PatcherStateKind.Idle);

            bool isInstalled =
                state.AppState?.InstalledVersionId.HasValue ?? false;

            PlayButton.interactable = isInstalled;

            CheckButtonText.text = isInstalled
                ? "Check for updates"
                : "Install";
        };
    }
}
}