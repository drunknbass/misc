"""Rebuild the editable RML from this original HUD composition. No external code."""
from pathlib import Path
from xml.sax.saxutils import escape
import math
root=Path(__file__).parent
items=[]; uid=100
props={'speed':(51,'Number',0),'speedText':(52,'String','00'),'nitro':(53,'Number',100),'nitroText':(54,'String','100%'),'position':(55,'String','01'),'lap':(56,'String','01'),'raceTime':(57,'String','00:00.00'),'truck':(58,'String','F-150 RAPTOR'),'surface':(59,'String','PACKED DIRT'),'recovery':(60,'Number',0),'boosting':(61,'Boolean','false')}
def ident():
 global uid
 uid+=1; return f'0:{uid}'
def bind(name,key,converter=''):
 return f'<DataBindContext sourcePathIds="0:40-0:{props[name][0]}" propertyKey="{key}"'+(f' converterId="{converter}"' if converter else '')+'/>'
def paint(color): return f'<Fill><SolidColor colorValue="{color}"/></Fill>'
def rect(name,x,y,w,h,color,r=0,binding='',opacity=None):
 i=ident(); items.append(f'<Shape id="{i}" name="{name}" x="{x}" y="{y}"'+(f' opacity="{opacity}"' if opacity is not None else '')+f'><Rectangle width="{w}" height="{h}" originX="0" originY="0" cornerRadiusTL="{r}">{binding}</Rectangle>{paint(color)}</Shape>');return i
def path(name,points,color,stroke=0,closed=True):
 i=ident();verts=''.join(f'<StraightVertex x="{x:.3f}" y="{y:.3f}"/>' for x,y in points)
 p=paint(color) if not stroke else f'<Stroke thickness="{stroke}" cap="round" join="round"><SolidColor colorValue="{color}"/></Stroke>'
 items.append(f'<Shape name="{name}" id="{i}"><PointsPath isClosed="{str(closed).lower()}">{verts}</PointsPath>{p}</Shape>');return i
def panel(name,x,y,w,h,color):
 return path(name,[(x,y),(x+w-16,y),(x+w,y+16),(x+w,y+h),(x+16,y+h),(x,y+h-16)],color)
def text(name,x,y,value,size=18,color='FFE9F0F2',weight='regular',prop=None,width=350):
 i=ident();style=ident()
 items.append(f'<Text name="{name}" id="{i}" x="{x}" y="{y}" width="{width}" height="{size*1.35}" sizingValue="fixed" overflowValue="fitFontSize" wrapValue="noWrap"><TextStylePaint id="{style}" fontSize="{size}" fontAssetId="0:{81 if weight=="bold" else 80}"><Fill><SolidColor colorValue="{color}"/></Fill></TextStylePaint><TextValueRun name="{name}Run" styleId="{style}" text="{escape(value)}">{bind(prop,268) if prop else ""}</TextValueRun></Text>')
# Draw back-to-front here; RML reverses it on export.
panel('Brand backing',28,24,370,82,'FF0B161D')
rect('Brand top keyline',44,24,115,3,'FFB9F26A')
path('Rally crest',[(44,66),(56,43),(68,66),(63,66),(56,53),(49,66)],'FFB9F26A')
path('Rally crest trail',[(43,72),(62,72),(68,78),(50,78)],'FF728C96')
text('Brand',82,32,'RAPTOR / RALLY',31,weight='bold',width=305)
text('Circuit',84,72,'COYOTE BASIN  /  DIRT SERIES',13,'FF94AEB8',width=285)
panel('Timing backing',566,24,476,82,'FF0B161D')
rect('Position stripe',566,40,3,48,'FFB9F26A')
text('Position label',586,32,'POSITION',12,'FF94AEB8',width=100)
text('Position value',584,45,'01',43,'FFB9F26A','bold','position',70)
text('Field size',642,65,'/ 04',18,'FF94AEB8',width=55)
rect('Lap divider',709,43,1,46,'FF304650')
text('Lap label',733,32,'LAP',12,'FF94AEB8',width=90)
text('Lap value',730,45,'01',43,weight='bold',prop='lap',width=70)
text('Lap total',788,65,'/ 03',18,'FF94AEB8',width=55)
rect('Clock divider',846,43,1,46,'FF304650')
text('Race clock label',868,32,'RACE TIME',12,'FF94AEB8',width=135)
text('Race clock',865,55,'00:00.00',30,weight='bold',prop='raceTime',width=167)
panel('Event badge',1250,24,208,58,'FF0B161D')
text('Event type',1270,31,'STADIUM / 01',21,weight='bold',width=178)
text('Session label',1271,59,'FOUR TRUCKS. ALL DIRT.',11,'FF94AEB8',width=180)
# Driver instrument cluster: distinct hierarchy, speed strip, boost reservoir.
panel('Driver panel shadow',32,650,426,194,'48000000')
panel('Driver panel',28,640,426,194,'FF0B161D')
rect('Driver top border',44,640,104,3,'FFB9F26A')
for i in range(18):
 path(f'Panel diagonal {i}',[(294+i*9,643),(284+i*9,673)],'183E5867',1,False)
text('Surface label',50,654,'PACKED DIRT',13,'FFB9F26A',prop='surface',width=260)
text('Speed digits',47,666,'00',103,weight='bold',prop='speedText',width=160)
text('Speed units',186,742,'MPH',18,'FF94AEB8',weight='bold',width=60)
rect('Boost divider',248,692,1,80,'FF304650')
text('Nitro label',271,678,'NITRO',14,'FF94AEB8',weight='bold',width=155)
text('Nitro remaining',270,704,'100%',32,weight='bold',prop='nitroText',width=162)
rect('Nitro reservoir',271,752,152,7,'FF263B47',3)
rect('Nitro available',271,752,152,7,'FF79D9E8',3,bind('nitro',20,'0:72'))
text('Boost key',272,769,'SPACE  /  BOOST',11,'FF94AEB8',width=158)
path('Boost bolt',[(418,678),(407,696),(415,696),(410,706),(426,688),(417,688)],'FF79D9E8')
boost_id=panel('Nitro engaged rim',28,640,426,194,'18FF933F')
rect('Speed rail',50,803,380,5,'FF263B47',2)
rect('Speed fill',50,803,0,5,'FFB9F26A',2,bind('speed',20,'0:71'))
for i in range(15): rect(f'Speed notch {i}',50+i*27,798,2,15,'FF0B161D')
for i,v in enumerate(['0','20','40','60','80']): text(f'Speed scale {v}',48+i*92,815,v,10,'FF94AEB8',width=25)
panel('Truck label plate',28,600,304,31,'FF0B161D')
text('Selected truck',45,605,'F-150 RAPTOR',16,weight='bold',prop='truck',width=273)
# Warning is a real host state, not a repeating animation.
warning=[]; before=len(items)
panel('Recovery backing',556,126,488,70,'FF192026')
rect('Recovery edge',556,138,4,44,'FFFFB75E')
path('Recovery icon',[(575,165),(587,143),(600,165)],'FFFFB75E')
rect('Recovery mark',586,150,2,7,'FF172129');rect('Recovery dot',586,160,2,2,'FF172129')
text('Recovery title',614,136,'OFF COURSE',18,'FFFFB75E','bold',width=360)
text('Recovery help',614,161,'Drive through a barrier to rejoin the track.',16,'FFE9F0F2',width=406)
warning=items[before:];items=items[:before]
items.append('<Node name="Off-course guidance" opacity="0">'+bind('recovery',18)+''.join(reversed(warning))+'</Node>')
# Boost state machine, a short non-looping response; the host passes false in reduced-motion mode.
def transition(dest,val):
 return f'<StateTransition stateToId="{dest}" duration="120"><TransitionViewModelCondition><TransitionPropertyViewModelComparator><BindablePropertyBoolean>{bind("boosting",634)}</BindablePropertyBoolean></TransitionPropertyViewModelComparator><TransitionValueBooleanComparator value="{val}"/></TransitionViewModelCondition></StateTransition>'
sm=f'<StateMachine name="RaceHUD" id="0:7"><StateMachineLayer name="Boost response"><AnyState x="200" y="-100"/><ExitState x="400" y="-100"/><EntryState x="0" y="0"><StateTransition stateToId="0:12"/></EntryState><AnimationState id="0:12" animationId="0:20" x="200" y="0">{transition("0:13","true")}</AnimationState><AnimationState id="0:13" animationId="0:21" x="400" y="0">{transition("0:12","false")}</AnimationState></StateMachineLayer></StateMachine>'
animations=''.join(f'<LinearAnimation name="{name}" id="0:{aid}" duration="12"><KeyedObject objectId="{boost_id}"><KeyedProperty propertyKey="18"><KeyFrameDouble value="{opacity}" frame="0"/></KeyedProperty></KeyedObject></LinearAnimation>' for name,aid,opacity in [('Ready',20,0),('Boost',21,1)])
vm='<ViewModel name="RaceHUD" id="0:40" defaultInstanceId="0:41">'+''.join(f'<ViewModelProperty{kind} name="{name}" id="0:{idx}"/>' for name,(idx,kind,value) in props.items())+'<ViewModelInstance name="Default" id="0:41" exports="true">'+''.join(f'<ViewModelInstance{kind} viewModelPropertyId="0:{idx}" propertyValue="{escape(str(value))}"/>' for name,(idx,kind,value) in props.items())+'</ViewModelInstance></ViewModel>'
xml='<Rive version="1" kind="fragment"><Artboard name="RaceHUD" id="0:2" width="1600" height="900" styleId="0:5" defaultStateMachineId="0:7" viewModelId="0:40" viewModelInstanceId="0:41"><LayoutComponentStyle id="0:5"/>'+''.join(reversed(items))+sm+animations+'</Artboard>'+vm+'<DataConverterRangeMapper id="0:71" name="Speed width" minInput="0" maxInput="80" minOutput="0" maxOutput="380" clampLower="true" clampUpper="true"/><DataConverterRangeMapper id="0:72" name="Nitro width" minInput="0" maxInput="100" minOutput="0" maxOutput="152" clampLower="true" clampUpper="true"/><FontAsset name="Source Sans 3 Regular" id="0:80" file="SourceSans3-Regular.ttf"/><FontAsset name="Source Sans 3 Bold" id="0:81" file="SourceSans3-Bold.ttf"/></Rive>'
from xml.dom import minidom
(root/'scene.rml').write_text(minidom.parseString(xml).toprettyxml(indent='  '))
(root/'rive.yaml').write_text('name: racing-hud\nmain: RaceHUD\nexclude:\n  - build\n  - previews\n')
print('Wrote editable RaceHUD source:',len(items),'visual elements')
