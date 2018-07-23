using System;
using System.Collections.Generic;
using System.Text;

namespace EMailManejador
{
    class EmailOutlook
    {
        //private Outlook.Application oApp;
        //private Outlook._NameSpace oNameSpace;
        //private Outlook.MAPIFolder oOutboxFolder;
        //public EmailOutlook()
        //{
        //    //Return a reference to the MAPI layer
        //    oApp = new Outlook.Application();

        //    oApp = new Outlook.Application();
        //    oNameSpace = oApp.GetNamespace("MAPI");

        //    //Profile: This is a string value that indicates what MAPI profile to use for logging on. Set this to null if using the currently logged on user, or set to an empty string ("") if you wish to use the default Outlook Profile.
        //    //Password: The password for the indicated profile. Set to null if using the currently logged on user, or set to an empty string ("") if you wish to use the default Outlook Profile password.
        //    //ShowDialog: Set to True to display the Outlook Profile dialog box.
        //    //NewSession: Set to True to start a new session or set to False to use the current session.

        //    oNameSpace.Logon(null, null, true, true);

        //    //gets defaultfolder for my Outlook Outbox
        //    oOutboxFolder = oNameSpace.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderOutbox);
        //}

        //public void addToOutBox(string toValue, string subjectValue, string bodyValue)
        //{
        //    Outlook._MailItem oMailItem = (Outlook._MailItem)oApp.CreateItem(Outlook.OlItemType.olMailItem);
        //    oMailItem.To = toValue;
        //    oMailItem.Subject = subjectValue;
        //    oMailItem.Body = bodyValue;
        //    oMailItem.SaveSentMessageFolder = oOutboxFolder;
        //    //uncomment this to also save this in your draft
        //    //oMailItem.Save();
        //    //adds it to the outbox
        //    oMailItem.Send();
        //} 
    }
}
