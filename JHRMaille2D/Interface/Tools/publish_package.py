"""Create the public update ZIP/feed after building and updating the bundle manifest."""
from pathlib import Path
import hashlib,json,sys,zipfile,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
bundle=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else root/'JHR.Panneaux.bundle'
output=Path(sys.argv[2]).resolve() if len(sys.argv)>2 else root/'Updates'
manifest=bundle/'PackageContents.xml';document=ET.parse(manifest).getroot();version=document.attrib['AppVersion']
module=document.find('.//ComponentEntry').attrib['ModuleName'].removeprefix('./')
assert module==f'Contents/Windows/JhrMaillePlugin_v{version.replace(".","_")}_final.dll'
dll=bundle/module;data=dll.read_bytes()
for marker in ('C:/Users/','C:\\Users\\','OneDrive'):
 assert marker.encode() not in data and marker.encode('utf-16le') not in data,'Local/private path in DLL'
output.mkdir(parents=True,exist_ok=True);package=output/f'JHR.Panneaux-{version}.zip'
with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED) as archive:
 for path in (manifest,dll):archive.write(path,'JHR.Panneaux.bundle/'+path.relative_to(bundle).as_posix())
feed={'schema':1,'version':version,'package':package.name,'sha256':hashlib.sha256(package.read_bytes()).hexdigest(),'size':package.stat().st_size,'autoCadSeries':'R26.0','notes':'Bandeau corrigé aux différentes échelles Windows ; vérification et installation des mises à jour GitHub.'}
(output/'latest.json').write_text(json.dumps(feed,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Update package:',package.name,'Version:',version,'SHA256:',feed['sha256'])
