using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Data;
using NLog;

namespace AdCampaign.Model
{
/// <summary>
/// Select cards for campaign and create advertisement campaign
/// </summary>
    public class Campaign : IDisposable
    {
        NpgsqlConnection Connection { get; set; }
        NpgsqlTransaction Transaction { get; set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();
        int count;
        int smslist_id;

#region advertisement fields
        public int CampaignId { get; private set; }
        public string CampaignName { get; set; }
        public string MessageText { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateStop { get; set; }
        public int? Discount { get; set; }
        public int? DiscountType { get; set; }
        public decimal? Sum { get; set; }
        long? Bonus { get; set; }
        public bool Send { get; set; }
        public string Info { get; set; }
        public string Alpha { get; set; }
        public bool IsTranslit { get; set; }
        public string TestMessage { get; private set; }
#endregion

        public int Count 
        {
            get 
            {
                if (Connection.State == ConnectionState.Open)
                    return count;
                else throw new InvalidOperationException("There is no avalueble transaction");
            }
            private set
            {
                count = value;
            }
        }

        public Campaign(NpgsqlConnection connection) 
        {
            if (connection == null) throw new ArgumentNullException("Parameter connection can't be null");
            Connection = connection;
            Info = String.Empty;
            IsTranslit = false;
        }

        public Campaign(int campId, NpgsqlConnection connection)
            : this(connection)
        {
            try
            {
                connection.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(String.Format("SELECT * FROM advertisement a LEFT JOIN advertsinfo ai ON a.id = ai.adverts_id WHERE id = {0}", campId), connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            this.CampaignId = campId;
                            this.CampaignName = reader["campaign_name"] as string;
                            this.MessageText = reader["msg"] as string;
                            this.DateStart = Convert.ToDateTime(reader["dstart"]);
                            this.DateStop = Convert.ToDateTime(reader["dstop"]);
                            this.Discount = Convert.IsDBNull(reader["discount"]) ? null : (Nullable<int>)Convert.ToInt32(reader["discount"]);
                            this.DiscountType = Convert.IsDBNull(reader["discounttype"]) ? null : (Nullable<int>)Convert.ToInt32(reader["discounttype"]);
                            this.Sum = Convert.IsDBNull(reader["summa"]) ? null : (Nullable<decimal>)(Convert.ToDecimal(reader["summa"]) * 0.01m);
                            this.Bonus = Convert.IsDBNull(reader["bonus"]) ? null : (Nullable<long>)Convert.ToInt64(reader["bonus"]);
                            this.Send = Convert.ToBoolean(reader["send"]);
                            this.Info = reader["info"] as string;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new ApplicationException(ex.Message);
            }
            finally
            {
                connection.Close();
            }
            
        }

#region Start and Stop campaign

        void ChangeDiscounts()
        {
            string startquery = String.Empty;
            if (!this.DiscountType.HasValue && this.Discount.HasValue)
            {
                startquery = String.Format(
                    @"UPDATE cardinfo ci SET discount = 
                        (SELECT charge_to FROM chargesmapping WHERE chargesdynamic_id = {0}::smallint AND charge_from = ci.discount), discounttype = 0" +
                    (this.Bonus.HasValue ? String.Format(", bonus = {0}::bigint", this.Bonus.Value) : "") +
                    " WHERE card_no IN (SELECT card_no FROM campaign WHERE compaign_id = {1}::integer);", this.Discount.Value, this.CampaignId);
                if (this.Sum.HasValue && this.Sum.Value > 0)
                {
                    startquery += String.Format(@"INSERT INTO transinfo(
	                            card_no, kind, summa, restaurant, rkdate, rkunit, 
                                rkchecka, vatsuma, vatprca, vatsumb, vatprcb, vatsumc, vatprcc, 
                                vatsumd, vatprcd, vatsume, vatprce, vatsumf, vatprcf, vatsumg, 
                                vatprcg, vatsumh, vatprch, rkcheckb)
                                (SELECT  card_no, 0, {0}::bigint, 777, now()::date, 105, 7777, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7777 
                                FROM campaign WHERE compaign_id = {1}::integer)", (long)(this.Sum.Value * 100), this.CampaignId);
                }
            }
            else if (this.DiscountType.HasValue && this.Discount.HasValue)
            {
                startquery = String.Format(
                    @"UPDATE cardinfo SET discount = {0}::smallint, discounttype = {1}::smallint" +
                    (this.Bonus.HasValue ? String.Format(", bonus = {0}::bigint", this.Bonus.Value) : "") +
                    " WHERE card_no IN (SELECT card_no FROM campaign WHERE compaign_id = {2}::integer);", this.Discount.Value, this.DiscountType.Value, this.CampaignId);
                if (this.Sum.HasValue && this.Sum.Value > 0)
                {
                    startquery += String.Format(@"INSERT INTO transinfo(
	                            card_no, kind, summa, restaurant, rkdate, rkunit, 
                                rkchecka, vatsuma, vatprca, vatsumb, vatprcb, vatsumc, vatprcc, 
                                vatsumd, vatprcd, vatsume, vatprce, vatsumf, vatprcf, vatsumg, 
                                vatprcg, vatsumh, vatprch, rkcheckb)
                                (SELECT  card_no, 0, {0}::bigint, 777, now()::date, 105, 7777, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7777 
                                FROM campaign WHERE compaign_id = {1}::integer)", (long)(this.Sum.Value * 100), this.CampaignId);
                }
            }
            else
            {
                if (this.Bonus.HasValue)
                    startquery = String.Format(@"UPDATE cardinfo SET bonus = {0}::bigint WHERE card_no IN (SELECT card_no FROM campaign WHERE compaign_id = {1}::integer);", this.Bonus.Value, this.CampaignId);
                if (this.Sum.HasValue && this.Sum.Value > 0)
                {
                    startquery += String.Format(@"INSERT INTO transinfo(
	                            card_no, kind, summa, restaurant, rkdate, rkunit, 
                                rkchecka, vatsuma, vatprca, vatsumb, vatprcb, vatsumc, vatprcc, 
                                vatsumd, vatprcd, vatsume, vatprce, vatsumf, vatprcf, vatsumg, 
                                vatprcg, vatsumh, vatprch, rkcheckb)
                                (SELECT  card_no, 0, {0}::bigint, 777, now()::date, 105, 7777, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7777 
                                FROM campaign WHERE compaign_id = {1}::integer)", (long)(this.Sum.Value * 100), this.CampaignId);
                }
            }
            using (NpgsqlCommand command = new NpgsqlCommand(startquery, Connection))
            {
                command.ExecuteNonQuery();
            }
        }

        void SendSMS()
        {
            NpgsqlCommand command;
            if (String.IsNullOrEmpty(this.Alpha)) throw new InvalidOperationException("Parameter Alpha can't be empty.");
            using (command = new NpgsqlCommand(@"INSERT INTO smslist (describe, smslist_name) 
                                                            (SELECT ai.info, ad.campaign_name 
                                                             FROM advertisement ad 
                                                             INNER JOIN advertsinfo ai ON ad.id = ai.adverts_id WHERE ad.id = :id)
                                                             RETURNING id;", Connection))
            {
                command.Parameters.AddWithValue("id", CampaignId);
                smslist_id = Convert.ToInt32(command.ExecuteScalar());
            }
            using (command = new NpgsqlCommand(@"INSERT INTO smsmsg (smslist_id, msgtext, describe, datebegin, datecancel, statusopen, alpha)
                                                               (SELECT :smslist_id, ad.msg, ai.info, ad.dstart, ad.dstop, false, :alpha 
                                                                FROM advertisement ad
                                                                INNER JOIN advertsinfo ai ON ad.id = ai.adverts_id WHERE ad.id = :id);", Connection))
            {
                command.Parameters.AddWithValue("smslist_id", smslist_id);
                command.Parameters.Add("alpha", NpgsqlTypes.NpgsqlDbType.Varchar, 10);
                command.Parameters["alpha"].Value = Alpha;
                command.Parameters.AddWithValue("id", CampaignId);
                command.ExecuteNonQuery();
            }
            List<Array> listqueries = new List<Array>();
            if (!this.IsTranslit && Transliteration.IsTranslit(this.MessageText)) this.IsTranslit = true;
            using (command = new NpgsqlCommand(GetCardsQuery(), Connection))
            {
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string message = BuildSmsString(reader);
                        
                        Array arr = new [] { new NpgsqlParameter("card_no", reader["card_no"]), new NpgsqlParameter("destaddress", reader["mobile"]), 
                            new NpgsqlParameter("smslist_id", smslist_id), new NpgsqlParameter("msgtext", message), new NpgsqlParameter("alpha", this.Alpha) };
                        listqueries.Add(arr);
                    }
                }
            }
            using (command = new NpgsqlCommand(@"INSERT INTO smsdadr (card_no, destaddress, deliveryid, smslist_id, msgtext, alpha)
                                                          VALUES (@card_no, @destaddress, 0, @smslist_id, @msgtext, @alpha)", Connection))
            {
                foreach (Array q in listqueries)
                {
                        command.Parameters.AddRange(q);
                        command.ExecuteNonQuery();
                }
            }
        }

        void ConfirmStart()
        {
            using (NpgsqlCommand command = new NpgsqlCommand(String.Format("UPDATE smsmsg SET statusopen = true WHERE smslist_id = {0};UPDATE advertisement SET send = false WHERE id = {1};",
                this.smslist_id, this.CampaignId), Connection))
            {
                command.ExecuteNonQuery();
            }
        }

        public void StartAction()
        {
            if (!this.Send) throw new InvalidOperationException("You can't send sms in this campaign.");
            if (Connection.State != ConnectionState.Closed) throw new InvalidOperationException("Some operation with data base is already run.");
            Connection.Open();
            NpgsqlTransaction t = Connection.BeginTransaction();
            try
            {
                ChangeDiscounts();
                SendSMS();
                ConfirmStart();
                t.Commit();
                this.Send = false;
            }
            catch (Exception ex)
            {
                t.Rollback();
                logger.ErrorException(ex.Message, ex);
                throw new Exception(ex.Message);
            }
            finally
            {
                t.Dispose();
                Connection.Close();
            }
        }

        public void StopAction()
        {
            if (this.Send) throw new InvalidOperationException("Can't stop unruning campaign.");
            if (Connection.State != ConnectionState.Closed) throw new InvalidOperationException("Some operation with data base is already run.");
            Connection.Open();
            try
            {
                string stopquery;
                stopquery = String.Format(
                            @"DO $$DECLARE 
                            r campaign%rowtype;
                            BEGIN
                                FOR r IN SELECT * FROM campaign  WHERE compaign_id = {0}::integer
                                LOOP
                                UPDATE cardinfo SET discount = r.cdiscount, discounttype = r.cdiscounttype WHERE card_no = r.card_no; 
                                PERFORM set_group_sum(r.group_no);
                                END LOOP;
                                UPDATE smsmsg SET statusopen = false WHERE smslist_id = (SELECT l.id FROM smslist l INNER JOIN advertisement a ON l.smslist_name = a.campaign_name AND a.id = {0}::integer);
                            END$$;", this.CampaignId);

                using (NpgsqlCommand command = new NpgsqlCommand(stopquery, Connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new Exception(ex.Message);
            }
            finally
            {
                Connection.Close();
            }
        }
#endregion

#region Update and delete campaign
        public void UpdateCampaign()
        {
            if (Connection.State != ConnectionState.Closed) throw new InvalidOperationException("Some operation with data base is already run.");
            Connection.Open();
            NpgsqlTransaction t = Connection.BeginTransaction();
            try
            {
                string updatequery = @"UPDATE advertisement 
                            SET campaign_name = :campaign_name, msg = :msg, 
                                dstart = :dstart, dstop = :dstop, 
                                discount = :discount, discounttype = :discounttype, 
                                summa = :summa, bonus = :bonus
                            WHERE id = :id; UPDATE advertsinfo SET info = :info WHERE adverts_id = :id;";
                string updatecampaign = "UPDATE campaign SET  dstart = :dstart, dstop = :dstop WHERE compaign_id = :id;";
                if (Send) updatequery += updatecampaign;

                using (NpgsqlCommand command = new NpgsqlCommand(updatequery, Connection))
                {
                    command.Parameters.Add("campaign_name",NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                    command.Parameters["campaign_name"].Value = CampaignName;
                    command.Parameters.Add("msg", NpgsqlTypes.NpgsqlDbType.Varchar, 500);
                    command.Parameters["msg"].Value = MessageText;
                    command.Parameters.Add("dstart", NpgsqlTypes.NpgsqlDbType.Timestamp);
                    command.Parameters["dstart"].Value = DateStart;
                    command.Parameters.Add("dstop", NpgsqlTypes.NpgsqlDbType.Timestamp);
                    command.Parameters["dstop"].Value = DateStop;
                    command.Parameters.Add("discount", NpgsqlTypes.NpgsqlDbType.Smallint);
                    command.Parameters["discount"].Value = Discount;
                    command.Parameters.Add("discounttype", NpgsqlTypes.NpgsqlDbType.Smallint);
                    command.Parameters["discounttype"].Value = DiscountType;
                    command.Parameters.Add("summa", NpgsqlTypes.NpgsqlDbType.Bigint);
                    command.Parameters["summa"].Value = Sum;
                    command.Parameters.Add("bonus", NpgsqlTypes.NpgsqlDbType.Bigint);
                    command.Parameters["bonus"].Value = Bonus;
                    command.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Integer);
                    command.Parameters["id"].Value = CampaignId;
                    command.Parameters.Add("info", NpgsqlTypes.NpgsqlDbType.Text);
                    command.Parameters["info"].Value = Info;
                    command.ExecuteNonQuery();
                }
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw new Exception(ex.Message);
            }
            finally
            {
                Connection.Close();
            }
        }

        public void DeleteCampaign()
        {
            if (Connection.State != ConnectionState.Closed) throw new InvalidOperationException("Some operation with data base is already run.");
            if (!this.Send) throw new InvalidOperationException("You can't delete the campaign that has been already started.");
            Connection.Open();
            NpgsqlTransaction t = Connection.BeginTransaction();
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(@"DELETE FROM campaign WHERE compaign_id = :compaign_id;
                                                                                 DELETE FROM advertsinfo WHERE adverts_id = :compaign_id;
                                                                                 DELETE FROM advertisement WHERE id = :compaign_id", Connection))
                {
                    command.Parameters.Add("compaign_id", NpgsqlTypes.NpgsqlDbType.Integer);
                    command.Parameters["compaign_id"].Value = CampaignId;
                    command.ExecuteNonQuery();
                }
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw new Exception(ex.Message);
            }
            finally
            {
                Connection.Close();
            }
        }
#endregion

#region Creating new campaign
        public void BeginCampaign(string query)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("Query string is empty");
            if (Connection.State == ConnectionState.Open) throw new ApplicationException("Connection has already been opened");
            DateStart = DateTime.Now;
            DateStop = DateTime.Now.AddHours(24.01);
            Connection.Open();
            Transaction = Connection.BeginTransaction();
            using (NpgsqlCommand command = new NpgsqlCommand(String.Format("DROP TABLE IF EXISTS querytemplatetable;SELECT * INTO TEMP TABLE querytemplatetable FROM ({0}) t", query), Connection, Transaction))
            {
                Count = command.ExecuteNonQuery();
            }
        }

        public enum ExporFlag {CardsAndPhones, Phones}

        public void ExportToCsv(string path, ExporFlag flag)
        {
            if (Connection.State != ConnectionState.Open || Transaction == null) throw new InvalidOperationException("You must start transaction with BeginCampaign() before.");
            using (NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM querytemplatetable", Connection))
            {
                using (StreamWriter csvwriter = new StreamWriter(path, false))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (flag == ExporFlag.CardsAndPhones)
                            {
                                csvwriter.WriteLine(reader["mobile"].ToString() + ";" + reader["card_no"].ToString());
                            }
                            else
                            {
                                csvwriter.WriteLine(reader["mobile"].ToString());
                            }
                        }
                    }
                }
            }
        }

        public void Cancel()
        {
            if (Transaction == null) throw new InvalidOperationException("Transaction was not begun before.");
            Transaction.Rollback();
            Transaction.Dispose();
            Connection.Close();
        }

        void ComitTransaction()
        { 
            if (Transaction == null) throw new InvalidOperationException("Transaction was not begun before.");
            Transaction.Commit();
            Transaction.Dispose();
            Connection.Close();
        }

        public void Dispose()
        {
            Transaction.Dispose();
            Connection.Close();
        }

        public int CreateCampaign()
        {
            NpgsqlCommand command;
            object campaign_id;

            if (Connection.State != ConnectionState.Open || Transaction == null) throw new InvalidOperationException("You must start transaction with BeginCampaign() before.");
            if (String.IsNullOrEmpty(CampaignName) || String.IsNullOrEmpty(MessageText)) throw new InvalidOperationException("Some members, that are used in campaign creation, are empty.");
            using (command = new NpgsqlCommand("SELECT relname FROM pg_class WHERE relname = 'querytemplatetable';", Connection, Transaction))
            {
                if (command.ExecuteScalar() as string != "querytemplatetable")
                    throw new InvalidOperationException("Some troubles with temporary table that is used for creating campaign.");
            }
            // Greate new campaign
            using (command = new NpgsqlCommand(@"INSERT INTO advertisement (campaign_name, msg, dstart, dstop, discount, discounttype, summa, bonus, send) VALUES
                                        (:campaign_name, :msg, :dstart, :dstop, :discount, :discounttype, :summa, :bonus, true) RETURNING id", Connection))
            {
                command.Parameters.Add("campaign_name", NpgsqlTypes.NpgsqlDbType.Varchar, 200);
                command.Parameters["campaign_name"].Value = CampaignName;
                command.Parameters.Add("msg", NpgsqlTypes.NpgsqlDbType.Varchar, 500);
                command.Parameters["msg"].Value = MessageText;
                command.Parameters.Add("dstart", NpgsqlTypes.NpgsqlDbType.Timestamp);
                command.Parameters["dstart"].Value = DateStart;
                command.Parameters.Add("dstop", NpgsqlTypes.NpgsqlDbType.Timestamp);
                command.Parameters["dstop"].Value = DateStop;
                command.Parameters.Add("discount", NpgsqlTypes.NpgsqlDbType.Smallint);
                command.Parameters["discount"].Value = Discount;
                command.Parameters.Add("discounttype", NpgsqlTypes.NpgsqlDbType.Smallint);
                command.Parameters["discounttype"].Value = DiscountType;
                command.Parameters.Add("summa", NpgsqlTypes.NpgsqlDbType.Bigint);
                command.Parameters["summa"].Value = Sum;
                command.Parameters.Add("bonus", NpgsqlTypes.NpgsqlDbType.Bigint);
                command.Parameters["bonus"].Value = Bonus;
                campaign_id = command.ExecuteScalar();
            }

            CampaignId = Convert.ToInt32(campaign_id);
            this.Info = " (" + this.Count + " участвующих) " + this.Info;
            // information about campaign
            using (command = new NpgsqlCommand("INSERT INTO advertsinfo (adverts_id, info) VALUES (:adverts_id, :info)", Connection, Transaction))
            {
                command.Parameters.AddWithValue("adverts_id", CampaignId);
                command.Parameters.AddWithValue("info", Info);
                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Error when try to add record to advertsinfo");
            }
            // add cards into campaign
            string query = @"INSERT INTO campaign (card_no, dstart, dstop, cdiscount, cdiscounttype, group_no, parent_no, card1_no, card2_no, summa, mobile, compaign_id)
                (SELECT t.card_no, :dstart, :dstop, ci.discount, ci.discounttype, ci.group_no, ci.parent_no, ci.card1_no, ci.card2_no, ci.summa, t.mobile, :compaign_id
                FROM querytemplatetable t
                INNER JOIN cardinfo ci ON ci.card_no = t.card_no)";
            using (command = new NpgsqlCommand(query, Connection, Transaction))
            {
                command.Parameters.Add("dstart", NpgsqlTypes.NpgsqlDbType.Timestamp);
                command.Parameters["dstart"].Value = DateStart;
                command.Parameters.Add("dstop", NpgsqlTypes.NpgsqlDbType.Timestamp);
                command.Parameters["dstop"].Value = DateStop;
                command.Parameters.AddWithValue("compaign_id", CampaignId);
                int result = command.ExecuteNonQuery();
                this.ComitTransaction();
                return result;
            }
        }
#endregion

#region Overloading
        public static bool operator == (Campaign camp1, Campaign camp2)
        { 
            return (camp1 as object == null && camp2 as object == null) 
                || (camp1 as object != null && camp2 as object != null) && 
                camp1.CampaignName == camp2.CampaignName && camp1.MessageText == camp2.MessageText
                && camp1.DateStart == camp2.DateStart && camp1.DateStop == camp2.DateStop 
                && camp1.Discount == camp2.Discount && camp1.DiscountType == camp2.DiscountType
                && camp1.Sum == camp2.Sum && camp1.Send == camp2.Send && camp1.Info == camp2.Info 
                && camp1.Bonus == camp2.Bonus && camp1.CampaignId == camp2.CampaignId; 
        }

        public static bool operator != (Campaign camp1, Campaign camp2)
        {
            return !(camp1 == camp2);
        }

        public override bool Equals(object obj)
        {
            return this == (Campaign) obj;
        }

        public override int GetHashCode()
        {
            return this.CampaignId;
        }

        public Campaign Clone()
        {
            Campaign CampaignClone = new Campaign(Connection);
            CampaignClone.CampaignId = this.CampaignId;
            CampaignClone.CampaignName = this.CampaignName;
            CampaignClone.MessageText = this.MessageText;
            CampaignClone.DateStart = this.DateStart;
            CampaignClone.DateStop = this.DateStop;
            CampaignClone.Discount = this.Discount;
            CampaignClone.DiscountType = this.DiscountType;
            CampaignClone.Sum = this.Sum;
            CampaignClone.Bonus = this.Bonus;
            CampaignClone.Send = this.Send;
            CampaignClone.Info = this.Info;
            return CampaignClone;
        }
#endregion

#region useful functions
        string GetCardsQuery()
        {
            string messagequery = @"SELECT c.card_no, c.mobile{0}
                                    FROM advertisement ad
                                    INNER JOIN campaign c ON ad.id = c.compaign_id
                                    {1}
                                    WHERE ad.id = {2}::integer";
            string columns = "";
            string joins = "";
            if (MessageText.Contains("[:discount]"))
            {
                columns += ", ch.percent";
                joins += @"INNER JOIN cardinfo ci ON c.card_no = ci.card_no
                           INNER JOIN charges ch ON ci.discount = ch.charges AND ci.discounttype = ch.discounttype";
            }
            return String.Format(messagequery, columns, joins, this.CampaignId);
        }

        string BuildSmsString(NpgsqlDataReader reader)
        {
            string card_no;
            if (Convert.IsDBNull(reader["card_no"])) throw new NullReferenceException("Field card_no can't be null.");
            string message = MessageText.Replace("[:card_no]", card_no = reader["card_no"].ToString());
            if (reader.HasOrdinal("percent"))
            {
                if (Convert.IsDBNull(reader["percent"])) throw new InvalidOperationException(String.Format("Field chargesname return empty value where card_no = {0}", card_no));
                string percent = reader["percent"].ToString();
                message = message.Replace("[:discount]", percent + "%");
            }
            return message;
        }

        string GetTestCardsQuery()
        {
            string messagequery = @"SELECT c.card_no, c.mobile{0}
                                    FROM advertisement ad
                                    INNER JOIN campaign c ON ad.id = c.compaign_id
                                    {1}
                                    WHERE ad.id = {2}::integer ORDER BY card_no DESC LIMIT 1";
            string columns = "";
            string joins = "";

            if ( this.Discount.HasValue && MessageText.Contains("[:discount]"))
            {
                columns += ", ch.percent::character(2)";
                if (this.DiscountType.HasValue)
                {
                    joins += @"INNER JOIN charges ch ON ad.discount = ch.charges AND ad.discounttype = ch.discounttype";
                }
                else
                {
                    joins += @"INNER JOIN chargesmapping cm ON cm.chargesdynamic_id = ad.discount
                               INNER JOIN cardinfo ci ON ci.discount = cm.charge_from
                               INNER JOIN charges ch ON cm.charge_to = ch.charges AND ch.discounttype = 0";
                }
            }
            return String.Format(messagequery, columns, joins, this.CampaignId);
        }

        public void DoTestMessage()
        {
            if (String.IsNullOrEmpty(MessageText)) throw new InvalidOperationException("Can't testing empty message.");
            if (Connection.State != ConnectionState.Closed) throw new ApplicationException("Connection must be closed in campaing editor.");
            try
            {
                Connection.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(this.GetTestCardsQuery(), Connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            this.TestMessage = BuildSmsString(reader);
                            if (this.IsTranslit || Transliteration.IsTranslit(this.MessageText)) this.TestMessage = Transliteration.Front(this.TestMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new Exception("Error when try to test message", ex);
            }
            finally
            {
                Connection.Close();
            }
        }
#endregion
    }
}
