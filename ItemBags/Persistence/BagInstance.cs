﻿using ItemBags.Bags;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ItemBags.Persistence
{
    [XmlRoot(ElementName = "BagInstance", Namespace = "")]
    public class BagInstance
    {
        [XmlElement("InstanceId")]
        public int InstanceId { get; set; }
        [XmlElement("TypeId")]
        public string TypeId { get; set; }
        [XmlElement("Size")]
        public ContainerSize Size { get; set; }
        [XmlElement("Autofill")]
        public bool Autofill { get; set; }

#region Rucksack Properties
        [XmlElement("AutofillPriority")]
        public AutofillPriority AutofillPriority { get; set; }
        [XmlElement("SortProperty")]
        public SortingProperty SortProperty { get; set; }
        [XmlElement("SortOrder")]
        public SortingOrder SortOrder { get; set; }
        #endregion Rucksack Properties

#region Omni Bag Properties
        [XmlArray("NestedBags")]
        [XmlArrayItem("Bag")]
        public BagInstance[] NestedBags { get; set; }
#endregion Omni Bag Properties

        [XmlArray("Contents")]
        [XmlArrayItem("Item")]
        public BagItem[] Contents { get; set; }

        [XmlElement("IsCustomIcon")]
        public bool IsCustomIcon { get; set; }
        [XmlElement("OverriddenIcon")]
        public Rectangle OverriddenIcon { get; set; }

        public BagInstance()
        {
            InitializeDefaults();
        }

        public BagInstance(int Id, ItemBag Bag)
        {
            InitializeDefaults();
            this.InstanceId = Id;

            if (Bag is BoundedBag BoundedBag)
            {
                if (BoundedBag is BundleBag BundleBag)
                {
                    this.TypeId = BundleBag.BundleBagTypeId;
                }
                else
                {
                    this.TypeId = BoundedBag.TypeInfo.Id;
                }
                this.Autofill = BoundedBag.Autofill;
            }
            else if (Bag is Rucksack Rucksack)
            {
                this.TypeId = Rucksack.RucksackTypeId;
                this.Autofill = Rucksack.Autofill;
                this.AutofillPriority = Rucksack.AutofillPriority;
                this.SortProperty = Rucksack.SortProperty;
                this.SortOrder = Rucksack.SortOrder;
            }
            else if (Bag is OmniBag OmniBag)
            {
                this.TypeId = OmniBag.OmniBagTypeId;
                this.NestedBags = OmniBag.NestedBags.Select(x => new BagInstance(-1, x)).ToArray();
            }
            else
            {
                throw new NotImplementedException(string.Format("Logic for encoding Bag Type '{0}' is not implemented", Bag.GetType().ToString()));
            }

            this.Size = Bag.Size;
            if (Bag.Contents != null)
            {
                this.Contents = Bag.Contents.Where(x => x != null).Select(x => new BagItem(x)).ToArray();
            }

            if (Bag.IsUsingDefaultIcon() || !Bag.IconTexturePosition.HasValue)
            {
                this.IsCustomIcon = false;
                this.OverriddenIcon = new Rectangle();
            }
            else
            {
                this.IsCustomIcon = true;
                this.OverriddenIcon = Bag.IconTexturePosition.Value;
            }
        }

        internal bool TryDecode(Dictionary<string, BagType> IndexedBagTypes, out ItemBag Decoded)
        {
            //  Handle BundleBags
            if (this.TypeId == BundleBag.BundleBagTypeId)
            {
                Decoded = new BundleBag(this);
                return true;
            }
            //  Handle Rucksacks
            else if (this.TypeId == Rucksack.RucksackTypeId)
            {
                Decoded = new Rucksack(this);
                return true;
            }
            //  Handle OmniBags
            else if (this.TypeId == OmniBag.OmniBagTypeId)
            {
                Decoded = new OmniBag(this, IndexedBagTypes);
                return true;
            }
            //  Handle all other types of Bags
            else if (IndexedBagTypes.TryGetValue(this.TypeId, out BagType BagType))
            {
                BagSizeConfig SizeConfig = BagType.SizeSettings.FirstOrDefault(x => x.Size == this.Size);
                if (SizeConfig != null)
                {
                    Decoded = new BoundedBag(BagType, this);
                    return true;
                }
                else
                {
                    string Warning = string.Format("Warning - BagType with Id = {0} was found, but it does not contain any settings for Size={1}. Did you manually edit your {2} json file? The saved bag with InstanceId = {3} cannot be loaded without the corresponding settings for this size!",
                        this.TypeId, this.Size.ToString(), ItemBagsMod.BagConfigDataKey, this.InstanceId);
                    ItemBagsMod.ModInstance.Monitor.Log(Warning, LogLevel.Warn);
                    Decoded = null;
                    return false;
                }
            }
            else
            {
                string Warning = string.Format("Warning - no BagType with Id = {0} was found. Did you manually edit your {1} json file? The saved bag with InstanceId = {2} cannot be loaded without a corresponding type!",
                    this.TypeId, ItemBagsMod.BagConfigDataKey, this.InstanceId);
                ItemBagsMod.ModInstance.Monitor.Log(Warning, LogLevel.Warn);
                Decoded = null;
                return false;
            }
        }

        private void InitializeDefaults()
        {
            this.InstanceId = -1;
            this.TypeId = Guid.Empty.ToString();
            this.Size = ContainerSize.Small;
            this.Autofill = false;
            this.Contents = new BagItem[] { };
            this.IsCustomIcon = false;
            this.OverriddenIcon = new Rectangle();
            this.AutofillPriority = AutofillPriority.Low;
            this.SortProperty = SortingProperty.Similarity;
            this.SortOrder = SortingOrder.Ascending;
            this.NestedBags = new BagInstance[] { };
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) { InitializeDefaults(); }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
    }
}
