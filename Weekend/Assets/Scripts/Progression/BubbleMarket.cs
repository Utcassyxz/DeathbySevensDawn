using System;
using UnityEngine;
using DeathBySeventhsDawn.Core;

namespace DeathBySeventhsDawn.Progression
{
    [Serializable]
    public sealed class BubbleOffer
    {
        public string id, titleZh, titleEn;
        public float sanityCost, knowledgeGain;
        public bool enabled;
    }

    [Serializable]
    public sealed class BubbleOfferCatalog
    {
        public BubbleOffer[] offers;
    }

    public static class BubbleMarket
    {
        static BubbleOfferCatalog catalog;
        public static BubbleOffer[] Offers
        {
            get
            {
                if (catalog == null)
                {
                    var asset = Resources.Load<TextAsset>("BubbleOffers");
                    catalog = asset != null ? JsonUtility.FromJson<BubbleOfferCatalog>(asset.text) : new BubbleOfferCatalog();
                }

                return catalog.offers ?? new BubbleOffer[0];
            }
        }

        public static bool Purchase(string id)
        {
            if (NightDreamSequence.Instance == null || !NightDreamSequence.Instance.IsShop)
                return false;
            foreach (var offer in Offers)
                if (offer != null && offer.enabled && offer.id == id)
                    return GameManager.Instance.ExchangeSanity(offer.id, offer.sanityCost, offer.knowledgeGain);
            return false;
        }
    }
}
