using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ButtonRow : MonoBehaviour
{

    [Header("Runtime Properties (don't edit, please)")] 
    public UserSelection currentSelection;

    [Header("Callbacks")]
    public Action<UserSelection> selectionChanged;
    
    [Header("Scene References")]
    public Button tuna;
    public Button taco;
    public Button gull;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentSelection = UserSelection.None;
        
        tuna.onClick.AddListener(() =>
        {
            currentSelection = UserSelection.Tuna;
            selectionChanged?.Invoke(currentSelection);
        });
        taco.onClick.AddListener(() =>
        {
            currentSelection = UserSelection.Taco;
            selectionChanged?.Invoke(currentSelection);
        });
        gull.onClick.AddListener(() =>
        {
            currentSelection = UserSelection.Gull;
            selectionChanged?.Invoke(currentSelection);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Lock()
    {
        tuna.interactable = false;
        taco.interactable = false;
        gull.interactable = false;
    }
}
