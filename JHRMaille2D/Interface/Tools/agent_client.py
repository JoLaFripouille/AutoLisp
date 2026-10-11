"""Petit transport JSON pour le plugin chargé dans l'AutoCAD actif. Nécessite pywin32."""
import json,time,tempfile
from pathlib import Path

def send(request, *, drawing, timeout=60, tool="panel"):
    import pythoncom
    from win32com.client.dynamic import Dispatch
    if not drawing:raise ValueError("Chemin du dessin requis")
    if tool not in ("panel","frame"):raise ValueError("Outil inconnu : panel ou frame")
    pythoncom.CoInitialize()
    try:
        app=Dispatch(pythoncom.GetActiveObject("AutoCAD.Application").QueryInterface(pythoncom.IID_IDispatch))
        doc=Dispatch(app.ActiveDocument)
        doc._FlagAsMethod("GetVariable");doc._FlagAsMethod("SendCommand")
        if str(doc.FullName).replace("/", "\\").casefold()!=str(drawing).replace("/", "\\").casefold():
            raise RuntimeError("Le dessin actif n'est pas le dessin demandé")
        if int(doc.GetVariable("CMDACTIVE"))!=0:raise RuntimeError("Une commande AutoCAD est déjà en cours")
        req=dict(request);req["drawing"]=str(doc.FullName)
        with tempfile.TemporaryDirectory(prefix="jhr-panel-") as work:
            path=Path(work)/"request.json";out=Path(str(path)+".response.json")
            path.write_text(json.dumps(req,ensure_ascii=False),encoding="utf-8")
            if path.stat().st_size>2_000_000:raise ValueError("Requête trop volumineuse")
            command="JHRCADREAPI" if tool=="frame" else "JHRPANNEAUAPI"
            doc.SendCommand('_'+command+'\n"'+path.as_posix()+'"\n')
            end=time.monotonic()+timeout
            while time.monotonic()<end:
                if out.exists():
                    try:return json.loads(out.read_text(encoding="utf-8-sig"))
                    except (json.JSONDecodeError,PermissionError):pass
                pythoncom.PumpWaitingMessages();time.sleep(.05)
            raise TimeoutError("Pas de réponse : vérifier le chargement du plugin et la ligne de commande. Ne pas relancer une écriture sans relire le panneau.")
    finally:pythoncom.CoUninitialize()

if __name__=="__main__":
    import argparse
    cli=argparse.ArgumentParser();cli.add_argument("request",type=Path);cli.add_argument("--drawing",required=True);cli.add_argument("--tool",choices=("panel","frame"),default="panel")
    args=cli.parse_args()
    print(json.dumps(send(json.loads(args.request.read_text(encoding="utf-8-sig")),drawing=args.drawing,tool=args.tool),ensure_ascii=False))
