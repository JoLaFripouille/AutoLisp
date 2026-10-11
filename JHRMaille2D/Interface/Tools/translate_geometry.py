from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'GeometrySource'/'approved-algorithms.lsp'
class Sym(str):pass
class Pair:
 def __init__(self,a,b):self.a,self.b=a,b
tokens=re.findall(r';[^\n]*|"(?:\\.|[^"\\])*"|[()]|\'|[^\s()\']+',SOURCE.read_text(encoding='utf-8-sig'))
tokens=[t for t in tokens if not t.startswith(';')];pos=0
def parse():
 global pos
 t=tokens[pos];pos+=1
 if t=='(':
  a=[]
  while tokens[pos]!=')':
   if tokens[pos]=='.':
    pos+=1;b=parse();assert tokens[pos]==')';pos+=1
    for item in reversed(a):b=Pair(item,b)
    return b
   a.append(parse())
  pos+=1;return a
 if t=="'":return [Sym('quote'),parse()]
 if t.startswith('"'):return bytes(t[1:-1],'utf-8').decode('unicode_escape') if '\\' in t else t[1:-1]
 try:return float(t) if any(x in t.lower() for x in ('.','e')) else int(t)
 except ValueError:return Sym(t.lower())
forms=[]
while pos<len(tokens):forms.append(parse())
defs={f[1]:f for f in forms if isinstance(f,list) and f and f[0]=='defun'}
exclude={'wm:product-stamp','wm:product-current','wm:product-xdata','wm:poly','wm:block','wm:offset','wm:validate','wm:generate','wm:cleanup','wm:ask','wm:ui','rf:prepare','rf:offset','rf:panel-name','rf:panel-name-poly','rf:hidden-type','rf:generate','rf:dispatch'}
chosen={n:f for n,f in defs.items() if not n.startswith('c:') and n not in exclude}
def s(v):return json.dumps(v,ensure_ascii=False)
def quoted(x):
 if isinstance(x,Sym):
  if x in ('nil','t'):return 'null' if x=='nil' else 'true'
  return 'new Symbol('+s(x)+')'
 if isinstance(x,Pair):return 'new Cell('+quoted(x.a)+','+quoted(x.b)+')'
 if isinstance(x,list):return 'L('+','.join(quoted(v) for v in x)+')'
 if isinstance(x,str):return s(x)
 return str(x)+('d' if isinstance(x,float) else '')
calls=set()
def seq(exprs,c='c'):
 if not exprs:return 'null'
 return 'Run(()=>{'+''.join('_ = '+emit(x,c)+';' for x in exprs[:-1])+'return '+emit(exprs[-1],c)+';})'
def emit(x,c='c'):
 if isinstance(x,Sym):return {'nil':'null','t':'true','pi':'Math.PI'}.get(x,c+'.Get('+s(x)+')')
 if not isinstance(x,list):return quoted(x)
 if not x:return 'null'
 op=str(x[0]);a=x[1:]
 if op=='quote':
  if isinstance(a[0],list) and a[0] and a[0][0]=='lambda':return emit(a[0],c)
  return quoted(a[0])
 if op=='lambda':
  ns=c+'l';return '(Lambda)(argv=>{var '+ns+'=new Scope('+c+','+quoted([str(v) for v in a[0]])+',argv);return '+seq(a[1:],ns)+';})'
 if op=='setq':return 'Run(()=>{object? result=null;'+''.join('result='+c+'.Set('+s(a[i])+','+emit(a[i+1],c)+');' for i in range(0,len(a),2))+'return result;})'
 if op=='progn':return seq(a,c)
 if op=='if':return '(Truth('+emit(a[0],c)+')?'+emit(a[1],c)+':'+(emit(a[2],c) if len(a)>2 else 'null')+')'
 if op=='and':return 'Bool('+'&&'.join('Truth('+emit(v,c)+')' for v in a)+')'
 if op=='or':return 'Bool('+'||'.join('Truth('+emit(v,c)+')' for v in a)+')'
 if op=='cond':
  out='null'
  for q in reversed(a):out='(Truth('+emit(q[0],c)+')?'+(seq(q[1:],c) if len(q)>1 else emit(q[0],c))+':'+out+')'
  return out
 if op=='foreach':return 'Run(()=>{object? result=null;foreach(var item in Items('+emit(a[1],c)+')){'+c+'.Set('+s(a[0])+',item);result='+seq(a[2:],c)+';}return result;})'
 if op=='repeat':return 'Run(()=>{object? result=null;int count=I('+emit(a[0],c)+');for(int ri=0;ri<count;ri++){Check();result='+seq(a[1:],c)+';}return result;})'
 if op=='while':return 'Run(()=>{object? result=null;while(Truth('+emit(a[0],c)+')){Check();result='+seq(a[1:],c)+';}return result;})'
 calls.add(op);return 'Call('+c+','+s(op)+',new object?[]{'+','.join(emit(v,c) for v in a)+'})'
out=['// Generated C# translation of the approved geometric algorithms.','// No AutoLISP evaluation, COM, command dispatch or drawing writes.','namespace JhrMailleUi;','partial class GeometryRuntime {','void RegisterGeometry(){']
for name,f in chosen.items():
 args=f[2];i=args.index(Sym('/')) if Sym('/') in args else len(args);params=args[:i];locals=args[i+1:] if i<len(args) else []
 out.append('functions['+s(name)+']=(parent,argv)=>{var c=new Scope(parent,'+quoted([str(v) for v in params])+',argv,'+quoted([str(v) for v in locals])+');return '+seq(f[3:])+';};')
out+=['}','}']
(ROOT/'GeometryCompiled.cs').write_text('\n'.join(out),encoding='utf-8')
(ROOT/'GeometrySource').mkdir(exist_ok=True)
# A source snapshot for maintenance/reproducibility only; never loaded by AutoCAD.
# The approved snapshot is deliberately not overwritten.
print('compiled functions',len(chosen),'calls',sorted(calls-set(chosen)))
