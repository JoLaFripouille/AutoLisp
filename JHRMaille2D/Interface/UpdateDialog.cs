namespace JhrMailleUi;

internal sealed class UpdateDialog : Form
{
    readonly UpdateService service=new();readonly CancellationTokenSource cancellation=new();
    readonly Label status=new(){Dock=DockStyle.Fill,ForeColor=UiTheme.Muted,AutoSize=false};
    readonly Button install=new ModernButton{Text="Installer",Enabled=false},check=new ModernButton{Text="Vérifier"},close=new ModernButton{Text="Fermer"};
    UpdateRelease? release;bool busy;
    internal UpdateDialog()
    {
        Text="JHR Panneaux — Mises à jour";Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;BackColor=UiTheme.Canvas;
        ClientSize=new(550,325);MinimumSize=new(530,360);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24),ColumnCount=1,RowCount=4};
        layout.RowStyles.Add(new(SizeType.Absolute,62));layout.RowStyles.Add(new(SizeType.Absolute,40));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,46));
        layout.Controls.Add(UiTheme.Header("Mises à jour","Source : JoLaFripouille / AutoLisp sur GitHub"),0,0);
        layout.Controls.Add(new Label{Text="Version chargée : "+UpdateService.CurrentVersion.ToString(3),AutoSize=true,ForeColor=UiTheme.Ink},0,1);layout.Controls.Add(status,0,2);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Margin=Padding.Empty};
        UiTheme.Button(install,true);UiTheme.Button(check);UiTheme.Button(close);foreach(var b in new[]{close,install,check}){b.Margin=new(8,0,0,0);buttons.Controls.Add(b);}layout.Controls.Add(buttons,0,3);Controls.Add(layout);
        close.Click+=(_,_)=>Close();check.Click+=async(_,_)=>await Check();install.Click+=async(_,_)=>await Install();Shown+=async(_,_)=>await Check();
        FormClosing+=(_,_)=>cancellation.Cancel();FormClosed+=(_,_)=>service.Dispose();
    }
    void Busy(bool value){busy=value;check.Enabled=!value;install.Enabled=!value&&release!=null&&release.ParsedVersion>UpdateService.CurrentVersion&&(service.InstalledVersion()==null||service.InstalledVersion()<release.ParsedVersion);}
    async Task Check()
    {
        if(busy)return;Busy(true);status.Text="Vérification sur GitHub…";
        try{release=await service.CheckAsync(cancellation.Token);if(IsDisposed)return;var installed=service.InstalledVersion();
            status.ForeColor=UiTheme.Ink;status.Text=installed>UpdateService.CurrentVersion&&installed>=release.ParsedVersion?"Version "+installed.ToString(3)+" déjà installée. Redémarre AutoCAD après sauvegarde de tes plans.":release.ParsedVersion>UpdateService.CurrentVersion?"Nouvelle version : "+release.Version+"\n\n"+release.Notes+"\n\nElle sera utilisée au prochain démarrage d’AutoCAD.":"Tu utilises la dernière version publiée.";
        }catch(OperationCanceledException){}catch(Exception){if(!IsDisposed){release=null;status.ForeColor=Color.Firebrick;status.Text="Impossible de vérifier la version. Vérifie ta connexion Internet puis réessaie. Le plugin reste utilisable.";}}
        finally{if(!IsDisposed)Busy(false);}
    }
    async Task Install()
    {
        if(busy||release==null)return;Busy(true);status.Text="Téléchargement et vérification du paquet…";
        try{string message=await service.InstallAsync(release,cancellation.Token);if(!IsDisposed){status.ForeColor=UiTheme.Accent;status.Text=message;}}
        catch(OperationCanceledException){}catch(Exception error){if(!IsDisposed){status.ForeColor=Color.Firebrick;status.Text="Installation interrompue : "+error.Message;}}
        finally{if(!IsDisposed)Busy(false);}
    }
}
