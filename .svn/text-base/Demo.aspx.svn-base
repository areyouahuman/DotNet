<%@ Page Language="C#" AutoEventWireup="true" Inherits="HelloAYAH.Demo" %>

<%@ Import Namespace="Ayah.WebServiceIntegrationLibrary" %>
<form id="form1" runat="server">
<div id="ayahDiv">
    <% 
        if (this.IsPostBack)
        {
            string sessionSecret = this.Request.Form.Get("session_secret");
            if (WebServiceProxy.ScoreResult(sessionSecret))
            {
    %>
    <%= WebServiceProxy.RecordConversion(sessionSecret)%>
    <br />
    <h3>
        Congratulations - you ARE a human!</h3>
    <%
}
            else
            {
    %>
    <h3>
        Sorry - you did not prove to be a human.</h3>
    <%
        }
        }
    %>
    <%= WebServiceProxy.GetPublisherHTML()%>
    <br />
    <input type="submit" value="Click me when you're done to see if you are human!" />
</div>
</form>
