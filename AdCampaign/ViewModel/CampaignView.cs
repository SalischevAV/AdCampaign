using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdCampaign.Model;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using DrWPF.Windows.Data;
using NLog;


namespace AdCampaign.ViewModel
{
    class CampaignView : ViewModelBase, IDataErrorInfo
    {
        bool IsNewCampaign;
        Campaign NewCampaign { get; set; }
        Campaign NewCampaignClone { get; set; }
        public Dictionary<int, ChargeView> Charges { get; private set; }
        public Dictionary<int, ChargeView> DynamicCharges { get; private set; }
        public ObservableDictionary<int, AdvertisementView> Advertisements { get; private set; }
        public ObservableCollection<string> AlphaList { get; private set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();
        bool isStaticCharge = true;
        public bool IsStaticCharge 
        {
            get { return isStaticCharge; }
            set 
            {
                isStaticCharge = value;
                OnPropertyChanged("IsStaticCharge");
                OnPropertyChanged("CurrentCharge");
            }
        }
        public bool IsNotStaticCharge
        {
            get { return !isStaticCharge; }
            set
            {
                isStaticCharge = !value;
                OnPropertyChanged("IsNotStaticCharge");
                OnPropertyChanged("CurrentDynamicCharge");
            }
        }
        /*********************************************************************/
        public string TemporaryQueryString { get; set; } // it's only for tests
        /*********************************************************************/

        public int CampaignId { get { return NewCampaign.CampaignId; } }
        public string StartCampaignCommandError { get; private set; }
#region properties that will be viewed
        public string CampaignName 
        {
            get { return NewCampaign.CampaignName; }
            set
            {
                NewCampaign.CampaignName = value;
                OnPropertyChanged("CampaignName");
            }
        }
        public string MessageText 
        {
            get { return NewCampaign.MessageText; }
            set 
            {
                NewCampaign.MessageText = value;
                OnPropertyChanged("MessageText");
                OnPropertyChanged("MessageLength");
            }
        }
        public int MessageLength
        {
            get { return MessageText.Length; }
        }
        public DateTime DateStart 
        {
            get { return NewCampaign.DateStart; }
            set 
            {
                NewCampaign.DateStart = value;
                OnPropertyChanged("DateStart");
            } 
        }
        public DateTime DateStop 
        {
            get { return NewCampaign.DateStop; }
            set
            {
                NewCampaign.DateStop = value;
                OnPropertyChanged("DateStop");
            }
        }
        public ChargeView CurrentCharge
        {
            get 
            {
                if (isStaticCharge)
                {
                    return Charges[ChargeView.GenerateHash(NewCampaign.Discount, NewCampaign.DiscountType)];
                }
                else
                {
                    return Charges[ChargeView.GenerateHash(0, 0)];
                }
            }
            set 
            {
                if (isStaticCharge)
                {
                    NewCampaign.Discount = value.Discount;
                    NewCampaign.DiscountType = value.DiscountType;
                    OnPropertyChanged("CurrentCharge");
                }
            }
        }
        public ChargeView CurrentDynamicCharge
        {
            get
            {
                if (!isStaticCharge)
                {
                    return DynamicCharges[ChargeView.GenerateHash(NewCampaign.Discount, NewCampaign.DiscountType)];
                }
                else
                {
                    return DynamicCharges[0];
                }
            }
            set
            {
                if (!isStaticCharge)
                {
                    NewCampaign.Discount = value.Discount;
                    NewCampaign.DiscountType = value.DiscountType;
                    OnPropertyChanged("CurrentDynamicCharge");
                }
            }
        }
        public decimal? Sum 
        {
            get { return NewCampaign.Sum; }
            set
            {
                NewCampaign.Sum = value;
                OnPropertyChanged("Sum");
            }
        }
        public bool Send 
        {
            get { return NewCampaign.Send; }
            set
            {
                NewCampaign.Send = value;
                OnPropertyChanged("Send");
            }
        }
        public string Info 
        {
            get { return NewCampaign.Info; }
            set
            {
                NewCampaign.Info = value;
                OnPropertyChanged("Info");
            } 
        } 
        public int Count
        {
            get { return NewCampaign.Count; }
        }

        int advertsindex;
        public int AdvertsIndex
        {
            get { return advertsindex; }
            set
            {
                advertsindex = value;
                OnPropertyChanged("AdvertsIndex");
            }
        }
        AdvertisementView curradverts = new AdvertisementView(-1, null);
        public AdvertisementView CurrentAdvertisenment
        {
            get { return curradverts; }
            set 
            {
                curradverts = value;
                OnPropertyChanged("CurrentAdvertisement");
                OnCurrentAdvertisementChanged();
            }
        }
        public string Alpha
        {
            get { return NewCampaign.Alpha; }
            set
            {
                NewCampaign.Alpha = value;
                OnPropertyChanged("Alpha");
            }
        }
        CampaignResultView campres;
        public CampaignResultView CampaignResult 
        {
            get { return campres; } 
            private set
            {
                campres = value;
                OnPropertyChanged("CampaignResult");
            }
        }
        public bool IsTranslit 
        {
            get { return NewCampaign.IsTranslit; }
            set { NewCampaign.IsTranslit = value; }
        }
        public string TestMessage
        {
            get { return NewCampaign.TestMessage; }
        }
        public string TestMessageLength
        {
            get 
            {
                if (Transliteration.IsTranslit(TestMessage))
                {
                    if (TestMessage.Length <= 160) return String.Format("{0} ({1})", TestMessage.Length, 1);
                    else return String.Format("{0} ({1})", TestMessage.Length, TestMessage.Length / 153 + 1);
                }
                else
                {
                    if (TestMessage.Length <= 70) return String.Format("{0} ({1})", TestMessage.Length, 1);
                    else return String.Format("{0} ({1})", TestMessage.Length, TestMessage.Length / 67 + 1);
                }
            }
        }
        public ICommand TranslitCommand { get; private set; }
        public ICommand ConfirmCampaignCreation { get; private set; }
        public ICommand CancelCampaignCreation { get; private set; }
        public ICommand UpdateCampaignCommand { get; private set; }
        public ICommand StartCampaignCommand { get; private set; }
        public ICommand StopCampaignCommand { get; private set; }
        public ICommand DoTestMessageCommand { get; private set; }
        public ICommand DeleteCampaignCommand { get; private set; }
        public ICommand ExportPhonesToCsv { get; private set; }
        public ICommand ExportPhonesAndCardsToCsv { get; private set; }
#endregion

#region Constructors for view
        CampaignView(NpgsqlConnection connection, bool isnew, bool isEditible)
        {
            TranslitCommand = new BaseCommand(p => { MessageText = Transliteration.Front(MessageText); this.IsTranslit = true; },
                p => this.IsValidForTranslit);
            IsNewCampaign = isnew;
            AlphaList =  new ObservableCollection<string>(new string [] {"MAFIA", "YAKITORIYA", "VITAEMO"});
        }

        /// <summary>
        /// Constructor for editing campaign
        /// </summary>
        /// <param name="connection"></param>
        public CampaignView(NpgsqlConnection connection, bool isEditible) 
            : this(connection, false, isEditible)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed) connection.Open();
                Advertisements = new ObservableDictionary<int, AdvertisementView>();
                using (NpgsqlCommand command = new NpgsqlCommand(String.Format("SELECT id, campaign_name FROM advertisement WHERE send = {0} ORDER BY id; ", isEditible), connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AdvertisementView advert = new AdvertisementView(Convert.ToInt32(reader["id"]), reader["campaign_name"] as string);
                            Advertisements.Add(advert.Id, advert);
                        }
                    }
                    ChargeView.FillChargesCollections(this, connection);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new Exception(ex.Message);
            }
            connection.Close();
            CurrentAdvertisementChanged += () => 
            {
                if (curradverts.Id < 0) return;
                UiServices.SetBusyState();
                NewCampaign = new Campaign(curradverts.Id, connection);
                NewCampaignClone = NewCampaign.Clone();
                if (NewCampaign.DiscountType.HasValue) IsStaticCharge = true;
                else IsNotStaticCharge = true;
                OnPropertyChanged("CampaignName");
                OnPropertyChanged("MessageText");
                OnPropertyChanged("DateStart");
                OnPropertyChanged("DateStop");
                OnPropertyChanged("Sum");
                OnPropertyChanged("Send");
                OnPropertyChanged("Info");
                OnPropertyChanged("Alpha");
                OnPropertyChanged("TestMessage");
                OnPropertyChanged("MessageLength");
                if (!NewCampaign.Send)
                {
                    CampaignResult = new CampaignResultView(this, connection);
                }
                UiServices.SetBusyState();
            };
            UpdateCampaignCommand = new BaseCommand(p =>
            {
                UiServices.SetBusyState();
                NewCampaign.UpdateCampaign(); 
                NewCampaignClone = NewCampaign.Clone();
                CurrentAdvertisenment.CampaignName = NewCampaign.CampaignName;
                UiServices.SetBusyState();
            },
                p => this.IsValidForUpdateCampaign);
            StartCampaignCommandError = String.Empty;
            StartCampaignCommand = new BaseCommand(p => 
            {
                StartCampaignCommandError = String.Empty;
                try
                {
                    UiServices.SetBusyState();
                    NewCampaign.StartAction();
                    UiServices.SetBusyState();
                }
                catch
                {
                    StartCampaignCommandError = "Ошибка, подробные сведения в логах.";
                }
            },
                p => this.IsValidForStartCampaign);
            StopCampaignCommand = new BaseCommand(p => 
            {
                UiServices.SetBusyState();
                NewCampaign.StopAction();
                if (CampaignResult != null)
                {
                    CampaignResult.IsStatusOpen = false;
                }
                UiServices.SetBusyState();
            }, p => this.IsValidForStopCampaign);
            DoTestMessageCommand = new BaseCommand(p => { NewCampaign.DoTestMessage(); OnPropertyChanged("TestMessage"); OnPropertyChanged("TestMessageLength"); }, 
                p => this.IsValidForStartCampaign);
            DeleteCampaignCommand = new BaseCommand(p => 
            {
                UiServices.SetBusyState();
                NewCampaign.DeleteCampaign(); 
                Advertisements.Remove(this.CampaignId);
                if (Advertisements.Count > 0) AdvertsIndex = 0;
                UiServices.SetBusyState();
            }, p => this.IsValidToDeleteCampaign);
        }

        /// <summary>
        /// Constructor for campaign creation
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        public CampaignView(string query, NpgsqlConnection connection) 
            : this(connection, true, true)
        {
            NewCampaign = new Campaign(connection);
            TemporaryQueryString = query;
            ConfirmCampaignCreation = new BaseCommand(p =>
            {
                UiServices.SetBusyState();
                NewCampaign.CreateCampaign();
                UiServices.SetBusyState();
            }, p => this.IsValidForCampaignCreation);
            CancelCampaignCreation = new BaseCommand(p => NewCampaign.Cancel(), p => true);
            ExportPhonesToCsv = new BaseCommand(p => NewCampaign.ExportToCsv(p.ToString(), Campaign.ExporFlag.Phones), p => true);
            ExportPhonesAndCardsToCsv = new BaseCommand(p => NewCampaign.ExportToCsv(p.ToString(), Campaign.ExporFlag.CardsAndPhones), p => true);
            UiServices.SetBusyState();
            NewCampaign.BeginCampaign(query);
            ChargeView.FillChargesCollections(this, connection);
            UiServices.SetBusyState();
        }
        /// <summary>
        /// Constructor for viewing campaign results
        /// </summary>
        /// <param name="campaign_id"></param>
        /// <param name="connection"></param>
        public CampaignView(int campaign_id, NpgsqlConnection connection, bool isEditible)
            : this(connection, isEditible)
        {
            CurrentAdvertisenment = Advertisements[campaign_id];
        }
#endregion

#region validating form for commands
        bool IsValidForCampaignCreation
        {
            get { return !String.IsNullOrEmpty(this.CampaignName) && !String.IsNullOrEmpty(this.MessageText); }
        }
        bool IsValidForStartCampaign
        {
            get { return NewCampaign != null && NewCampaign == NewCampaignClone && !String.IsNullOrEmpty(Alpha) && NewCampaign.DiscountType.HasValue == isStaticCharge; }
        }
        bool IsValidForUpdateCampaign
        {
            get 
            { 
                return NewCampaign != null && NewCampaign != NewCampaignClone 
                    && NewCampaign.DiscountType.HasValue == isStaticCharge 
                    && !String.IsNullOrEmpty(this.MessageText) && !String.IsNullOrEmpty(this.CampaignName); 
            }
        }
        bool IsValidForStopCampaign
        {
            get { return NewCampaign != null && !NewCampaign.Send; }
        }
        bool IsValidForTranslit
        {
            get { return NewCampaign != null && !String.IsNullOrEmpty(this.MessageText) && !Transliteration.IsTranslit(this.MessageText); }
        }
        bool IsValidToDeleteCampaign
        {
            get { return NewCampaign != null && NewCampaign.Send; }
        }
#endregion

#region helper classes
        public class ChargeView
        {
            public int? Discount { get; set; }

            public int? DiscountType { get; set; }

            public string Name { get; set; }

            public ChargeView(int? discount, int? discounttype, string name)
            {
                Discount = discount;
                DiscountType = discounttype;
                Name = name;
            }

            public static void FillChargesCollections(CampaignView campaign, NpgsqlConnection connection)
            {
                if (connection == null || connection.State == System.Data.ConnectionState.Closed) throw new ArgumentNullException("Connection is null or closed, when you try to get all charges");
                campaign.Charges = new Dictionary<int, ChargeView>();
                campaign.DynamicCharges = new Dictionary<int, ChargeView>();
                campaign.DynamicCharges.Add(0, new ChargeView(null, null, "пусто"));
                using (NpgsqlCommand command = new NpgsqlCommand(@"SELECT chargesname, charges, discounttype FROM charges WHERE discounttype = 0 AND percent > 0 ORDER BY percent;
                                                                   SELECT chargesdynamic_id, chargesname FROM chargesdynamic", connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ChargeView ch = new ChargeView(Convert.ToInt32(reader["charges"]), Convert.ToInt32(reader["discounttype"]), reader["chargesname"] as string);
                            campaign.Charges.Add(ch.GetHashCode(), ch);
                        }
                        reader.NextResult();
                        while (reader.Read())
                        {
                            ChargeView ch = new ChargeView(Convert.ToInt32(reader["chargesdynamic_id"]), null, reader["chargesname"] as string);
                            campaign.DynamicCharges.Add(ch.GetHashCode(), ch);
                        }
                    }
                }
            }

            public static int GenerateHash(int? discount, int? discounttype)
            {
                if (discount.HasValue && discounttype.HasValue)
                    return (discount.Value.ToString() + discount.Value.ToString()).GetHashCode();
                else if (discount.HasValue && !discounttype.HasValue)
                    return discount.Value.ToString().GetHashCode();
                else return 0;
            }

            public override int GetHashCode()
            {
                return GenerateHash(this.Discount, this.DiscountType);
            }
        }

        public class AdvertisementView
        {
            public int Id { get; private set; }
            public string CampaignName { get; set; }
            public AdvertisementView(int id, string name)
            {
                Id = id;
                CampaignName = name;
            }

            public override bool Equals(object obj)
            {
                AdvertisementView ad = obj as AdvertisementView;
                if (ad == null) return false;
                return this.Id == ad.Id;
            }

            public override int GetHashCode()
            {
                return this.Id;
            }
        }

        public class CampaignResultView  : ViewModelBase
        {
            public int AllCards { get; private set; }
            public int Comings { get; private set; }
            public int Cards { get; private set; }
            public decimal Sum { get; private set; }
            public int DeliveredSms { get; private set; }
            public string DiscountName { get; private set; }
            bool isstatusopen;
            public bool IsStatusOpen 
            {
                get { return isstatusopen; }
                set
                {
                    isstatusopen = value;
                    OnPropertyChanged("IsStatusOpen");
                }
            }
           

            public CampaignResultView(CampaignView camp, NpgsqlConnection connection)
            {
                if (camp.Send) throw new InvalidOperationException("Wrong using of class CampaignResult. Campaing must be sent");
                if (connection == null) throw new InvalidOperationException("Connection  can't be null (error in CampaingResult constructor).");
                if (camp.isStaticCharge) DiscountName = camp.CurrentCharge.Name;
                else DiscountName = camp.CurrentDynamicCharge.Name;
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();
                    string resultquery = String.Format(@"
                                                    SELECT COUNT(*) as count FROM campaign WHERE compaign_id = {0}::int;
                                                    SELECT SUM(deliv) FROM (SELECT 1 as deliv FROM smsstatus WHERE deliry ILIKE '%stat:DELIVRD%' AND list_id = 
                                                    (SELECT sl.id FROM smslist sl INNER JOIN advertisement ad ON sl.smslist_name = ad.campaign_name AND ad.id = {0}::int) GROUP BY addr_id) t;
                                                    SELECT sm.statusopen FROM smslist sl 
                                                    INNER JOIN advertisement ad ON sl.smslist_name = ad.campaign_name 
                                                    INNER JOIN smsmsg sm On sl.id = sm.smslist_id
                                                    WHERE ad.id = {0}::int;
                                                    SELECT COUNT(c.card_no) as count, SUM(ti.summa) * 0.01 as sum
                                                    FROM transinfo ti
                                                    INNER JOIN campaign c ON ti.card_no = c.card_no 
                                                    AND compaign_id = {0}::int AND ti.modifydate BETWEEN '{1}'::timestamp AND '{2}'::timestamp AND ti.kind  = 3;
                                                    SELECT COUNT(card_no) as count
                                                    FROM
                                                    (SELECT c.card_no
                                                    FROM transinfo ti
                                                    INNER JOIN campaign c ON ti.card_no = c.card_no 
                                                    AND compaign_id = {0}::int AND ti.modifydate BETWEEN '{1}'::timestamp AND '{2}'::timestamp AND ti.kind  = 3 GROUP BY c.card_no) t; ",
                                                       camp.CampaignId, camp.DateStart.ToString("o"), camp.DateStop.ToString("o"));
                    using (NpgsqlCommand command = new NpgsqlCommand(resultquery, connection))
                    {
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AllCards = Convert.IsDBNull(reader["count"]) ? 0 : Convert.ToInt32(reader["count"]);
                            }
                            reader.NextResult();
                            while (reader.Read())
                            {
                                DeliveredSms = Convert.IsDBNull(reader["sum"]) ? 0 : Convert.ToInt32(reader["sum"]);
                            }
                            reader.NextResult();
                            while (reader.Read())
                            {
                                IsStatusOpen = Convert.IsDBNull(reader["statusopen"]) ? false : Convert.ToBoolean(reader["statusopen"]);
                            }
                            reader.NextResult();
                            while (reader.Read())
                            {
                                Comings = Convert.IsDBNull(reader["count"]) ? 0 : Convert.ToInt32(reader["count"]);
                                Sum = Convert.IsDBNull(reader["sum"]) ? 0 : Convert.ToDecimal(reader["sum"]);
                            }
                            reader.NextResult();
                            while (reader.Read())
                            {
                                Cards = Convert.IsDBNull(reader["count"]) ? 0 : Convert.ToInt32(reader["count"]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorException(ex.Message, ex);
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    connection.Close();
                }
            }
        }
#endregion

#region Event
        private event Action CurrentAdvertisementChanged;
        void OnCurrentAdvertisementChanged()
        {
            if (CurrentAdvertisementChanged != null)
                CurrentAdvertisementChanged();
        }
#endregion

#region IDataErrorInfo
        string error;
        public string Error
        {
            get { return error; }
            private set
            {
                error = value;
                OnPropertyChanged("Error");
            }
        }
        public string this[string columnName]
        {
            get
            {
                Error = null;
                switch (columnName)
                { 
                    case "CampaignName":
                        if(String.IsNullOrEmpty(this.CampaignName)) Error = "Поле не должно быть пустым";
                        break;
                    case "MessageText":
                        if (String.IsNullOrEmpty(this.MessageText)) Error = "Поле не должно быть пустым";
                        break;
                    case "CurrentCharge":
                        if (this.IsStaticCharge && NewCampaign.DiscountType.HasValue != isStaticCharge) Error = "Выберите скидку";
                        break;
                    case "CurrentDynamicCharge":
                        if (this.IsNotStaticCharge && NewCampaign.DiscountType.HasValue != isStaticCharge) Error = "Выберите скидку";
                        break;
                }
                return Error;
            }
        }
#endregion
    }
}
