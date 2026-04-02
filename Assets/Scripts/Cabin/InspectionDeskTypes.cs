using System;
using System.Collections.Generic;
using UnityEngine;

public enum TicketBand
{
    None,
    Short,
    Medium,
    Long,
    DayRider
}

public enum InspectionDeskZoneKind
{
    None,
    SharedTray,
    FloatTray,
    Bin,
    ReviewArea
}

public enum InspectionDeskItemKind
{
    IdCard,
    Ticket,
    Cash,
    Clutter,
    Note,
    Tip,
    Evidence
}

public enum InspectionDeskClickTopic
{
    Generic,
    MissingId,
    MissingTicket,
    MissingPayment,
    IdCard,
    IdPhoto,
    IdName,
    IdDob,
    IdExpiry,
    IdNumber,
    Ticket,
    TicketValidity,
    TicketRoute,
    Money,
    MoneyAmount,
    MoneyAuthenticity,
    PassengerFace,
    PassengerHair,
    PassengerClothes,
    PassengerBehaviour
}

[Serializable]
public sealed class InspectionDeskQuestionOption
{
    public string id;
    public string label;

    public InspectionDeskQuestionOption() { }

    public InspectionDeskQuestionOption(string newId, string newLabel)
    {
        id = newId;
        label = newLabel;
    }
}

[Serializable]
public sealed class InspectionDeskItemState
{
    public string uniqueId;
    public InspectionDeskItemKind kind;
    public string title;
    public string subtitle;
    public int moneyValuePence;
    public bool isFake;
    public bool isPassengerOwned = true;
    public bool isImportant = true;
    public bool isTrash;
    public bool isEvidenceReference;
    public bool autoAddWhenLeftOnDesk;
    public bool isTemplateSource;
    public InspectionDeskClickTopic defaultTopic = InspectionDeskClickTopic.Generic;
    public List<InspectionDeskClickTopic> supportedTopics = new List<InspectionDeskClickTopic>();
    public string artKey;
    public Vector2 preferredSize;
    public bool preferArtOnly = true;

    public InspectionDeskItemState Clone()
    {
        return new InspectionDeskItemState
        {
            uniqueId = uniqueId,
            kind = kind,
            title = title,
            subtitle = subtitle,
            moneyValuePence = moneyValuePence,
            isFake = isFake,
            isPassengerOwned = isPassengerOwned,
            isImportant = isImportant,
            isTrash = isTrash,
            isEvidenceReference = isEvidenceReference,
            autoAddWhenLeftOnDesk = autoAddWhenLeftOnDesk,
            isTemplateSource = isTemplateSource,
            defaultTopic = defaultTopic,
            supportedTopics = supportedTopics != null
                ? new List<InspectionDeskClickTopic>(supportedTopics)
                : new List<InspectionDeskClickTopic>(),
            artKey = artKey,
            preferredSize = preferredSize,
            preferArtOnly = preferArtOnly
        };
    }
}
