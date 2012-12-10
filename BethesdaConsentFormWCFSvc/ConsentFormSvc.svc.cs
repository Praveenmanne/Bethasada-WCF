using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.ServiceModel;
using BethesdaConsentFormWCFSvc.DocumentConverterService;
using Microsoft.SharePoint;

namespace BethesdaConsentFormWCFSvc
{
    [ServiceContract] //This Attribute used to define the interface
    public class ConsentFormSvc
    {
        [OperationContract] //This Attribute used to define the method inside of interface
        public void AddTreatment(Treatment treatment)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
                {
                    sqlConnection.Open();
                    SqlTransaction transaction = sqlConnection.BeginTransaction();
                    try
                    {
                        int consentType = GetConsentTypeId(sqlConnection, transaction, treatment._consentType.ToString());
                        int trackingID = GetTrackingId(sqlConnection, transaction, treatment._trackingInformation._device, treatment._trackingInformation._iP);
                        int doctorsAndProceduresID = GetDoctorsAndProcedures(sqlConnection, transaction, treatment._doctorAndPrcedures);
                        int signaturesID = GetSignatures(sqlConnection, transaction, treatment._signatureses);

                        // SqlCommand cmdTreatment = new SqlCommand("insert into Treatment(PatentId,ConsentType,IsPatientunabletosign,Unabletosignreason,TrackingID,Signatures,DoctorandProcedure,TransaltedBy,Date) values(" + treatment._patientId + "," + consentType + "," + (treatment._isPatientUnableSign == true ? 1 : 0) + ",'" + (string.IsNullOrEmpty(treatment._unableToSignReason) ? "" : treatment._unableToSignReason) + "'," + trackingID + "," + signaturesID + "," + doctorsAndProceduresID + ",'" + (string.IsNullOrEmpty(treatment._translatedBy) ? "" : treatment._translatedBy) + "','" + DateTime.Now + "')", sqlConnection, transaction);
                        SqlCommand cmdTreatment = new SqlCommand("insert into Treatment(PatentId,ConsentType,IsPatientunabletosign,IsStatementOfConsentAccepted,IsAutologousUnits,IsDirectedUnits,Unabletosignreason,TrackingID,Signatures,DoctorandProcedure,TransaltedBy,Date) values(" + treatment._patientId + "," + consentType + "," + (treatment._isPatientUnableSign == true ? 1 : 0) + ",'" + (treatment._IsStatementOfConsentAccepted == true ? 1 : 0) + ",'" + (treatment._IsAutologousUnits == true ? 1 : 0) + ",'" + (treatment._IsDirectedUnits == true ? 1 : 0) + ",'" + (string.IsNullOrEmpty(treatment._unableToSignReason) ? "" : treatment._unableToSignReason) + "'," + trackingID + "," + signaturesID + "," + doctorsAndProceduresID + ",'" + (string.IsNullOrEmpty(treatment._translatedBy) ? "" : treatment._translatedBy) + "','" + DateTime.Now + "')", sqlConnection, transaction);
                        cmdTreatment.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [OperationContract]
        public Treatment GetTreatment(string patientId, ConsentType consentType)
        {
            // open connection to sql server
            Treatment treatment = new Treatment();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
                {
                    sqlConnection.Open();
                    SqlCommand cmdConsentID = new SqlCommand("select ID from ConsentType where [Name]='" + consentType.ToString() + "'", sqlConnection);
                    string consentID = "0";
                    using (var readId = cmdConsentID.ExecuteReader())
                    {
                        if (readId.Read())
                        {
                            consentID = readId["ID"].ToString();
                        }
                    }

                    // SqlDataAdapter daTreatment = new SqlDataAdapter("select PatentId,ConsentType,IsPatientunabletosign,Unabletosignreason,TrackingID,Signatures,DoctorandProcedure,TransaltedBy,Date from Treatment where PatentId=" + patientId + " and ConsentType=" + consentID + " and date=(select MAX(date) from Treatment where PatentId=" + patientId + " and ConsentType=" + consentID + ")", sqlConnection);
                    SqlDataAdapter daTreatment = new SqlDataAdapter("select PatentId,ConsentType,IsPatientunabletosign,IsStatementOfConsentAccepted,IsAutologousUnits,IsDirectedUnits,Unabletosignreason,TrackingID,Signatures,DoctorandProcedure,TransaltedBy,Date from Treatment where PatentId=" + patientId + " and ConsentType=" + consentID + " and date=(select MAX(date) from Treatment where PatentId=" + patientId + " and ConsentType=" + consentID + ")", sqlConnection);
                    DataSet dsTreatment = new DataSet();
                    daTreatment.Fill(dsTreatment);
                    if (dsTreatment.Tables[0].Rows.Count > 0)
                    {
                        treatment._patientId = patientId;
                        treatment._consentType = consentType;
                        treatment._isPatientUnableSign = (dsTreatment.Tables[0].Rows[0]["IsPatientunabletosign"].ToString() == "1" ? true : false);
                        treatment._unableToSignReason = dsTreatment.Tables[0].Rows[0]["Unabletosignreason"].ToString();
                        treatment._translatedBy = dsTreatment.Tables[0].Rows[0]["TransaltedBy"].ToString();
                        treatment._trackingInformation = GetTrackingInformation(sqlConnection, dsTreatment.Tables[0].Rows[0]["TrackingID"].ToString());

                        treatment._doctorAndPrcedures = GetDoctorsProceduresInformation(sqlConnection, dsTreatment.Tables[0].Rows[0]["DoctorandProcedure"].ToString());
                        treatment._signatureses = GetSignaturesInformation(sqlConnection, dsTreatment.Tables[0].Rows[0]["Signatures"].ToString());
                    }
                }
                return treatment;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [OperationContract]
        public void DeleteTables()
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
                {
                    sqlConnection.Open();
                    SqlCommand daTreatment = new SqlCommand("TRUNCATE TABLE Treatment", sqlConnection);
                    daTreatment.ExecuteNonQuery();
                    SqlCommand treatmentsignature = new SqlCommand("TRUNCATE TABLE Treatment_Signature", sqlConnection);
                    treatmentsignature.ExecuteNonQuery();
                    SqlCommand trackinginfo = new SqlCommand("TRUNCATE TABLE TrackingInformation", sqlConnection);
                    trackinginfo.ExecuteNonQuery();
                    SqlCommand cfprocedures = new SqlCommand("TRUNCATE TABLE CFProcedures", sqlConnection);
                    cfprocedures.ExecuteNonQuery();
                    SqlCommand doctors_procedures = new SqlCommand("TRUNCATE TABLE Doctor_Procedures", sqlConnection);
                    doctors_procedures.ExecuteNonQuery();
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [OperationContract]
        public PatientDetail GetPatientDetail(string patientNumber, string ConsentFormType)
        {
            PatientDetail patientDetail = null;
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select * from Patient where PatientId=" + patientNumber, sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                    {
                        if (dataset.Tables[0].Rows.Count > 0)
                        {
                            DataRow record = dataset.Tables[0].Rows[0];
                            patientDetail = new PatientDetail();
                            patientDetail.name = record["FullName"].ToString();
                            patientDetail.age = Convert.ToInt16(record["Age"]);
                            patientDetail.gender = record["Gender"].ToString();
                            patientDetail.MRHash = record["MR#"].ToString();
                            patientDetail.AttnDr = "Mr. Mathew Thomas";
                            patientDetail.AdmDate = DateTime.Now.AddDays(-2);
                            patientDetail.DOB = Convert.ToDateTime(record["BirthDate"]);
                            try
                            {
                                patientDetail.PrimaryDoctorId = record["PrimaryDoctor"].ToString();
                            }
                            catch (Exception)
                            {
                                patientDetail.PrimaryDoctorId = string.Empty;
                            }
                            try
                            {
                                patientDetail.AssociatedDoctorId = record["AssociatedDoctor"].ToString();
                            }
                            catch (Exception)
                            {
                                patientDetail.AssociatedDoctorId = string.Empty;
                            }
                            using (SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter("select * from Treatments where PatientID='" + patientNumber + "' AND ConsentFormType='" + ConsentFormType + "'", sqlConnection))
                            {
                                dataset = new DataSet();
                                sqlDataAdapter2.Fill(dataset);
                                if (dataset.Tables.Count > 0)
                                {
                                    if (dataset.Tables[0].Rows.Count > 0)
                                    {
                                        if (dataset.Tables[0].Rows[0]["Procedures"] != null)
                                            patientDetail.ProcedureName = dataset.Tables[0].Rows[0]["Procedures"].ToString();
                                        if (dataset.Tables[0].Rows[0]["UnableToSignReason"] != null)
                                            patientDetail.UnableToSignReason = dataset.Tables[0].Rows[0]["UnableToSignReason"].ToString();
                                        if (dataset.Tables[0].Rows[0]["Translatedby"] != null)
                                            patientDetail.Translatedby = dataset.Tables[0].Rows[0]["Translatedby"].ToString();
                                        patientDetail.StatementOfConsent = null;
                                        if (dataset.Tables[0].Rows[0]["StatementType"] != null && !string.IsNullOrEmpty(dataset.Tables[0].Rows[0]["StatementType"].ToString()))
                                        {
                                            patientDetail.StatementOfConsent = new StatementOfConsent();
                                            using (SqlDataAdapter sqlDataAdapterSC = new SqlDataAdapter("select * from StatementOfConsent where Id=" + dataset.Tables[0].Rows[0]["StatementType"], sqlConnection))
                                            {
                                                DataSet datasetSC = new DataSet();
                                                sqlDataAdapterSC.Fill(datasetSC);
                                                if (datasetSC.Tables.Count > 0)
                                                {
                                                    if (datasetSC.Tables[0].Rows.Count > 0)
                                                    {
                                                        if (datasetSC.Tables[0].Rows[0]["AutologousUnits"] != null && datasetSC.Tables[0].Rows[0]["AutologousUnits"].ToString() == "1")
                                                            patientDetail.StatementOfConsent.AutologousUnits = true;
                                                        if (datasetSC.Tables[0].Rows[0]["DirectedUnits"] != null && datasetSC.Tables[0].Rows[0]["DirectedUnits"].ToString() == "1")
                                                            patientDetail.StatementOfConsent.DirectedUnits = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return patientDetail;
        }

        private const string SiteUrl = "http://devsp1:20399";
        private const string DocumentLibary = "PatientConsentForms";

        [OperationContract]
        public void GenerateAndUploadPDFtoSharePoint(string RelativeUrl, string PatientId, ConsentType ConsentFormType)
        {
            DocumentConverterServiceClient client = null;
            try
            {
                string sourceFileName = null;
                byte[] sourceFile = null;
                client = OpenService("http://localhost:41734/Muhimbi.DocumentConverter.WebService/");
                OpenOptions openOptions = new OpenOptions();

                //** Specify optional authentication settings for the web page
                openOptions.UserName = "";
                openOptions.Password = "";

                // ** Specify the URL to convert
                openOptions.OriginalFileName = RelativeUrl;
                openOptions.FileExtension = "html";

                //** Generate a temp file name that is later used to write the PDF to
                sourceFileName = Path.GetTempFileName();
                File.Delete(sourceFileName);

                // ** Enable JavaScript on the page to convert.
                openOptions.AllowMacros = MacroSecurityOption.All;

                // ** Set the various conversion settings
                ConversionSettings conversionSettings = new ConversionSettings();
                conversionSettings.Fidelity = ConversionFidelities.Full;
                conversionSettings.PDFProfile = PDFProfile.PDF_1_5;
                conversionSettings.Quality = ConversionQuality.OptimizeForOnScreen;

                // ** Carry out the actual conversion
                byte[] convertedFile = client.Convert(sourceFile, openOptions, conversionSettings);

                //try
                //{
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (var site = new SPSite(SiteUrl))
                    {
                        using (var web = site.OpenWeb())
                        {
                            web.AllowUnsafeUpdates = true;

                            var list = web.Lists.TryGetList(DocumentLibary);
                            if (list != null)
                            {
                                var libFolder = list.RootFolder;

                                var patientDetails = GetPatientDetail(PatientId, ConsentFormType.ToString());

                                if (patientDetails != null)
                                {
                                    string fileName = ConsentFormType + patientDetails.MRHash + patientDetails.name + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                    switch (ConsentFormType)
                                    {
                                        case ConsentType.Surgical:
                                            {
                                                fileName = "SUR_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.BloodConsentOrRefusal:
                                            {
                                                fileName = "BLOOD_FEFUSAL_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.Cardiovascular:
                                            {
                                                fileName = "CARDIAC_CATH_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.Endoscopy:
                                            {
                                                fileName = "ENDO_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.OutsideOR:
                                            {
                                                fileName = "OUTSDE_OR_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.PICC:
                                            {
                                                fileName = "PICC_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                        case ConsentType.PlasmanApheresis:
                                            {
                                                fileName = "PLASMA_APHERESIS_CONSENT_" + patientDetails.MRHash + DateTime.Now.ToString("MMddyyyyHHmmss") + ".pdf";
                                                break;
                                            }
                                    }

                                    if (libFolder.RequiresCheckout) { try { SPFile fileOld = libFolder.Files[fileName]; fileOld.CheckOut(); } catch { } }
                                    var spFileProperty = new Hashtable();
                                    spFileProperty.Add("MR#", patientDetails.MRHash);
                                    spFileProperty.Add("Patient#", PatientId);
                                    spFileProperty.Add("Patient Name", patientDetails.name);
                                    spFileProperty.Add("DOB#", Microsoft.SharePoint.Utilities.SPUtility.CreateISO8601DateTimeFromSystemDateTime(patientDetails.DOB));
                                    spFileProperty.Add("Procedure Type", patientDetails.ProcedureName);
                                    spFileProperty.Add("Patient Information", patientDetails.name + " " + DateTime.Now.ToShortDateString());

                                    SPFile spfile = libFolder.Files.Add(fileName, convertedFile, spFileProperty, true);

                                    list.Update();

                                    if (libFolder.RequiresCheckout)
                                    {
                                        spfile.CheckIn("Upload Comment", SPCheckinType.MajorCheckIn);
                                        spfile.Publish("Publish Comment");
                                    }
                                }
                            }

                            web.AllowUnsafeUpdates = false;
                        }
                    }
                });
            }
            finally
            {
                CloseService(client);
            }
        }

        [OperationContract]
        public DataTable GetProcedures(ConsentType consentType)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                // open adapter and command for select query
                //using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Name,ConsentTypeID from CFProcedures order by Name asc", sqlConnection))
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select CFProcedures.Name as CFName,CFProcedures.ID as ID,ConsentType.Name as CTName from CFProcedures,ConsentType where CFProcedures.ConsentTypeID=ConsentType.ID and ConsentType.Name='" + consentType.ToString() + "'", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    return dataset.Tables[0];
                }
            }
        }

        [OperationContract]
        public DataTable GetConsentType()
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                // open adapter and command for select query
                //using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Name,ConsentTypeID from CFProcedures order by Name asc", sqlConnection))
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Name as CFName,ID from ConsentType order by Name asc", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    return dataset.Tables[0];
                }
            }
        }

        [OperationContract]
        public void AddProcedures(string procedureName, int consentTypeID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();
                SqlTransaction transaction = sqlConnection.BeginTransaction();
                try
                {
                    SqlCommand cmdTreatment = new SqlCommand("insert into CFProcedures(Name,ConsentTypeID) values('" + procedureName + "'," + consentTypeID + ")", sqlConnection, transaction);
                    cmdTreatment.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception(ex.Message);
                }
            }
        }

        [OperationContract]
        public void UpdateProcedures(string procedureName, int procedureID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();
                string sql = "Update CFProcedures set Name='" + procedureName + "'  where ID='" + procedureID + "'";
                var command = new SqlCommand(sql, sqlConnection);
                command.ExecuteNonQuery();
            }
        }

        [OperationContract]
        public void DeleteProcedure(int Id)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();
                string sql = "delete from CFProcedures where ID='" + Id + "'";
                var command = new SqlCommand(sql, sqlConnection);
                command.ExecuteNonQuery();
            }
        }

        [OperationContract]
        public List<string> ListProcedures(string Name)
        {
            var outPut = new List<string>();
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select CFProcedures.Name as CFName from CFProcedures,ConsentType where CFProcedures.ConsentTypeID=ConsentType.ID AND ConsentType.Name ='" + Name + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                    {
                        foreach (DataRow row in dataset.Tables[0].Rows)
                        {
                            outPut.Add(row[0].ToString());
                        }
                    }
                }
            }
            return outPut;
        }

        [OperationContract]
        public List<DoctorDetails> GetDoctorDetails(string consentName)
        {
            var output = new List<DoctorDetails>();

            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Physician.Fname as FName, Physician.Lname as LName,Physician.ConsentTypeID as ID from Physician,ConsentType where Physician.ConsentTypeID=ConsentType.ID AND  ConsentType.Name ='" + consentName + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                    {
                        foreach (DataRow row in dataset.Tables[0].Rows)
                        {
                            var doctorDetail = new DoctorDetails();
                            doctorDetail.ID = Convert.ToInt32(row["ID"].ToString());
                            doctorDetail.Fname = row["FName"].ToString();
                            doctorDetail.Lname = row["LName"].ToString();
                            output.Add(doctorDetail);
                        }
                    }
                }
            }
            return output;
        }

        [OperationContract]
        public DoctorDetails GetDoctorDetail(int ID)
        {
            var output = new DoctorDetails();
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Physician.Fname as FName, Physician.Lname as LName from Physician where Physician.ID  ='" + ID + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                    {
                        if (dataset.Tables[0].Rows.Count > 0)
                        {
                            DataRow row = dataset.Tables[0].Rows[0];

                            output.Fname = row["FName"].ToString();
                            output.Lname = row["LName"].ToString();
                            output.ID = ID;
                        }
                    }
                }
            }

            return output;
        }

        [OperationContract]
        public List<AssociatedDoctorDetails> GetAssociatedDoctors(int primaryDoctorID)
        {
            var output = new List<AssociatedDoctorDetails>();

            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select Physician.Fname as FName, Physician.Lname as LName from Physician where Physician.PCID ='" + primaryDoctorID + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                    {
                        foreach (DataRow row in dataset.Tables[0].Rows)
                        {
                            var doctorDetail = new AssociatedDoctorDetails();
                            doctorDetail.Fname = row["FName"].ToString();
                            doctorDetail.Lname = row["LName"].ToString();
                            output.Add(doctorDetail);
                        }
                    }
                }
            }
            return output;
        }

        [OperationContract]
        public void SavePdFFolderPath(ConsentType consenttype, string FolderPath)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select ConsentType.ID as ID from ConsentType where ConsentType.Name ='" + consenttype + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                        if (dataset.Tables[0].Rows.Count > 0)
                        {
                            DataRow record = dataset.Tables[0].Rows[0];
                            var id = record["ID"].ToString();

                            using (SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter("select count(*) from PDFExportPaths where ConsentID='" + id + "' ", sqlConnection))
                            {
                                dataset = new DataSet();
                                sqlDataAdapter2.Fill(dataset);
                                if (dataset.Tables.Count > 0)
                                    if (dataset.Tables[0].Rows.Count > 0)
                                    {
                                        int count = Convert.ToInt32(dataset.Tables[0].Rows[0][0]);
                                        if (count > 0)
                                        {
                                            string sql = "Update PDFExportPaths set ConsentID='" + id + "',Path='" + FolderPath + "' where ConsentID='" + id + "'";
                                            var command = new SqlCommand(sql, sqlConnection);
                                            command.ExecuteNonQuery();
                                        }
                                        else
                                        {
                                            string query = "Insert into PDFExportPaths values('" + id + "','" + FolderPath + "')";
                                            var Sqlcommand = new SqlCommand(query, sqlConnection);
                                            Sqlcommand.ExecuteNonQuery();
                                            sqlConnection.Close();
                                        }
                                    }
                            }
                        }
                }
            }
        }

        [OperationContract]
        public DataTable GetPdFFolderPath(ConsentType consenttype)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select ConsentType.ID as ID from ConsentType where ConsentType.Name ='" + consenttype + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                        if (dataset.Tables[0].Rows.Count > 0)
                        {
                            DataRow record = dataset.Tables[0].Rows[0];
                            var id = record["ID"].ToString();

                            using (SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter("select count(*) from PDFExportPaths where ConsentID='" + id + "' ", sqlConnection))
                            {
                                dataset = new DataSet();
                                sqlDataAdapter2.Fill(dataset);

                                if (dataset.Tables.Count > 0)
                                    if (dataset.Tables[0].Rows.Count > 0)
                                    {
                                        int count = Convert.ToInt32(dataset.Tables[0].Rows[0][0]);
                                        if (count > 0)
                                        {
                                            using (SqlDataAdapter sqlDataAdapter3 = new SqlDataAdapter("select PDFExportPaths.Path as Path from PDFExportPaths where PDFExportPaths.ConsentID ='" + id + "' ", sqlConnection))
                                            {
                                                dataset = new DataSet();
                                                sqlDataAdapter3.Fill(dataset);
                                                return dataset.Tables[0];
                                            }
                                        }
                                    }
                            }
                        }
                }
            }
            return null;
        }

        [OperationContract]
        public bool IsValidEmployee(string EmpID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();

                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select count(*) from EmployeeInformation where EmpID='" + EmpID + "' ", sqlConnection))
                {
                    DataSet dataset = new DataSet();
                    sqlDataAdapter.Fill(dataset);
                    if (dataset.Tables.Count > 0)
                        if (dataset.Tables[0].Rows.Count > 0)
                        {
                            int count = Convert.ToInt32(dataset.Tables[0].Rows[0][0]);
                            if (count > 0)
                            {
                                return true;
                            }
                        }
                }
                return false;
            }
        }

        private void InsertSeedRecords(bool Associated, bool PrimaryDoc, int ConsentTypeId, string FName, string LName, int PCID)
        {
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ToString()))
            {
                sqlConnection.Open();
                SqlCommand cmdTreatment = new SqlCommand("insert into Physican(Associated,Primary_Doctor,ConsentTypeID,Fname,Lname,PCID) values('" + (Associated == true ? 1 : 0) + ",'" + (PrimaryDoc == true ? 1 : 0) + ",'" + ConsentTypeId + ",'" + FName + ",'" + LName + "'," + PCID + "," + "')", sqlConnection);
                cmdTreatment.ExecuteNonQuery();
            }
        }

        private DocumentConverterServiceClient OpenService(string address)
        {
            DocumentConverterServiceClient client = null;

            try
            {
                BasicHttpBinding binding = new BasicHttpBinding();

                // ** Use standard Windows Authentication
                binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;

                // ** Allow long running conversions
                binding.SendTimeout = TimeSpan.FromMinutes(30);
                binding.ReceiveTimeout = TimeSpan.FromMinutes(30);

                // ** Allow file sizes of 50MB
                binding.MaxReceivedMessageSize = 50 * 1024 * 1024;
                binding.ReaderQuotas.MaxArrayLength = 50 * 1024 * 1024;
                binding.ReaderQuotas.MaxStringContentLength = 50 * 1024 * 1024;

                // ** We need to specify an identity (any identity) in order to get it past .net3.5 sp1
                EndpointIdentity epi = EndpointIdentity.CreateUpnIdentity("unknown");
                EndpointAddress epa = new EndpointAddress(new Uri(address), epi);

                client = new DocumentConverterServiceClient(binding, epa);

                client.Open();

                return client;
            }

            catch (Exception)
            {
                CloseService(client);
                throw;
            }
        }

        private void CloseService(DocumentConverterServiceClient client)
        {
            if (client != null && client.State == CommunicationState.Opened)
                client.Close();
        }

        private int GetConsentTypeId(SqlConnection con, SqlTransaction transaction, string concenType)
        {
            SqlCommand cmdConsentId = new SqlCommand("select ID from ConsentType where name='" + concenType + "'", con, transaction);
            using (var read = (SqlDataReader)cmdConsentId.ExecuteReader())
            {
                //dr = cmd.ExecuteReader();
                if (read.Read())
                    return Convert.ToInt32(read["ID"].ToString());
                else
                    return 0;
            }
        }

        private int GetTrackingId(SqlConnection con, SqlTransaction transaction, string device, string ip)
        {
            SqlCommand cmdInsert = new SqlCommand("insert into TrackingInformation(Device,IP) values('" + device + "','" + ip + "')", con, transaction);
            cmdInsert.ExecuteNonQuery();

            // where Device='" + device + "' and IP= '" + ip + "'
            SqlCommand cmdTrackingId = new SqlCommand("select max(ID) as ID from TrackingInformation", con, transaction);
            using (var read = (SqlDataReader)cmdTrackingId.ExecuteReader())
            {
                if (read.Read())
                    return Convert.ToInt32(read["ID"].ToString());
                else
                    return 0;
            }
        }

        private int GetDoctorsAndProcedures(SqlConnection con, SqlTransaction transaction, List<DoctorAndPrcedure> doctorsAndProcedures)
        {
            var uniqueId = 0;
            SqlCommand cmdUniquId = new SqlCommand("select isnull(max(UniqueID),0)+1 as UniqueID from Doctor_Procedures", con, transaction);
            using (var read = (SqlDataReader)cmdUniquId.ExecuteReader())
            {
                if (read.Read())
                    uniqueId = Convert.ToInt32(read["UniqueID"].ToString());
            }
            foreach (DoctorAndPrcedure dap in doctorsAndProcedures)
            {
                var strProcedures = dap._precedures.Split('#');
                var procedureId = 0;
                foreach (string procedure in strProcedures)
                {
                    if (!string.IsNullOrEmpty(procedure))
                    {
                        SqlDataAdapter cmdProcId = new SqlDataAdapter("select ID from CFProcedures where Name='" + procedure + "'", con);
                        DataSet dsProcId = new DataSet();
                        cmdProcId.SelectCommand.Transaction = transaction;
                        cmdProcId.Fill(dsProcId);
                        if (dsProcId.Tables[0].Rows.Count > 0)
                        {
                            procedureId = Convert.ToInt32(dsProcId.Tables[0].Rows[0][0].ToString());
                        }
                        else
                        {
                            SqlCommand cmdInsert = new SqlCommand("insert into CFProcedures(Name) values('" + procedure + "')", con, transaction);
                            cmdInsert.ExecuteNonQuery();

                            SqlDataAdapter cmdProcId1 = new SqlDataAdapter("select ID from CFProcedures where Name='" + procedure + "'", con);
                            DataSet dsProc = new DataSet();
                            cmdProcId1.SelectCommand.Transaction = transaction;
                            cmdProcId1.Fill(dsProc);
                            if (dsProc.Tables[0].Rows.Count > 0)
                            {
                                procedureId = Convert.ToInt32(dsProc.Tables[0].Rows[0][0].ToString());
                            }
                        }
                    }
                    SqlCommand cmdInsertDAP = new SqlCommand("insert into Doctor_Procedures(ProcID,UniqueID,PrimaryDoctorID) values(" + procedureId + "," + uniqueId + "," + dap._primaryDoctorId + ")", con, transaction);
                    cmdInsertDAP.ExecuteNonQuery();
                }
            }

            return uniqueId;
        }

        private int GetSignatures(SqlConnection con, SqlTransaction transaction, List<Signatures> signatures)
        {
            var tsID = 0;
            SqlCommand cmdTsId = new SqlCommand("select isnull(max(TSID),0)+1 as TSID from Treatment_Signature", con, transaction);
            using (var read = (SqlDataReader)cmdTsId.ExecuteReader())
            {
                if (read.Read())
                    tsID = Convert.ToInt32(read["TSID"].ToString());
            }

            foreach (Signatures sig in signatures)
            {
                var strSignatures = sig._signatureType.ToString();
                var signatureId = 0;
                if (!string.IsNullOrEmpty(strSignatures))
                {
                    SqlCommand cmdSigId = new SqlCommand("select ID from Signatures where Type='" + strSignatures + "'", con, transaction);
                    using (var read1 = (SqlDataReader)cmdSigId.ExecuteReader())
                    {
                        if (read1.Read())
                        {
                            signatureId = Convert.ToInt32(read1["ID"].ToString());
                        }
                    }
                    SqlCommand cmdInsert = new SqlCommand("insert into Treatment_Signature(SignatureId,TContent,Name,TSID) values(" + signatureId + ",'" + sig._signatureContent.ToString() + "','" + sig._name + "'," + tsID + ")", con, transaction);
                    cmdInsert.ExecuteNonQuery();
                }
            }
            return tsID;
        }

        private TrackingInformation GetTrackingInformation(SqlConnection con, string trackingId)
        {
            TrackingInformation tracking = new TrackingInformation();
            SqlCommand cmdTacking = new SqlCommand("select Device,IP from TrackingInformation where Id=" + trackingId + "", con);
            using (var read = cmdTacking.ExecuteReader())
            {
                if (read.Read())
                {
                    tracking._device = read["Device"].ToString();
                    tracking._iP = read["IP"].ToString();
                }
            }
            return tracking;
        }

        private List<DoctorAndPrcedure> GetDoctorsProceduresInformation(SqlConnection con, string UniqueId)
        {
            DoctorAndPrcedure doctorAndPrcedure = new DoctorAndPrcedure();
            List<DoctorAndPrcedure> listDr = new List<DoctorAndPrcedure>();
            SqlDataAdapter daDrProc = new SqlDataAdapter("select PrimaryDoctorID from Doctor_Procedures where UniqueID=" + UniqueId + "group by PrimaryDoctorID", con);
            DataSet dsDr = new DataSet();
            daDrProc.Fill(dsDr);
            if (dsDr.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < dsDr.Tables[0].Rows.Count; i++)
                {
                    doctorAndPrcedure._primaryDoctorId = dsDr.Tables[0].Rows[i]["PrimaryDoctorID"].ToString();
                    SqlDataAdapter daDrProc1 = new SqlDataAdapter("select ProcID from Doctor_Procedures where UniqueID=" + UniqueId + " and PrimaryDoctorID=" + dsDr.Tables[0].Rows[i]["PrimaryDoctorID"].ToString() + "", con);
                    DataSet dsDr1 = new DataSet();
                    daDrProc1.Fill(dsDr1);
                    if (dsDr1.Tables[0].Rows.Count > 0)
                    {
                        for (int j = 0; j < dsDr1.Tables[0].Rows.Count; j++)
                        {
                            SqlCommand cmdProcedure = new SqlCommand("select Name from CFProcedures where Id=" + dsDr1.Tables[0].Rows[j]["ProcID"].ToString() + "", con);
                            using (var read = cmdProcedure.ExecuteReader())
                            {
                                if (read.Read())
                                {
                                    doctorAndPrcedure._precedures = read["Name"].ToString() + "#" + doctorAndPrcedure._precedures;
                                }
                            }
                        }
                    }
                    listDr.Add(doctorAndPrcedure);
                }
            }
            return listDr;
        }

        private List<Signatures> GetSignaturesInformation(SqlConnection con, string signaturesId)
        {
            List<Signatures> listSig = new List<Signatures>();
            Signatures signatures = new Signatures();
            SqlDataAdapter daSig = new SqlDataAdapter("select SignatureId,TContent,Name from Treatment_Signature where TSID=" + signaturesId + "", con);
            DataSet dsSig = new DataSet();
            daSig.Fill(dsSig);
            if (dsSig.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < dsSig.Tables[0].Rows.Count; i++)
                {
                    signatures._name = dsSig.Tables[0].Rows[i]["Name"].ToString();
                    signatures._signatureContent = dsSig.Tables[0].Rows[i]["TContent"].ToString();
                    SqlCommand cmdProcedure = new SqlCommand("select [Type] from Signatures where Id='" + dsSig.Tables[0].Rows[i]["SignatureId"].ToString() + "'", con);
                    using (var read = cmdProcedure.ExecuteReader())
                    {
                        if (read.Read())
                        {
                            signatures._signatureType = (SignatureType)Enum.Parse(typeof(SignatureType), read["Type"].ToString());
                        }
                    }
                    listSig.Add(signatures);
                }
            }
            return listSig;
        }
    }
}