"""Write editable SCML metadata; this script never modifies image files."""
from pathlib import Path
import xml.etree.ElementTree as E
import json

NAME = 'baiye_compact_transit'
ROOT = Path(__file__).resolve().parents[1]
layout = json.loads((ROOT/'art/station-layout.json').read_text(encoding='utf-8'))
out = ROOT / 'art/source' / NAME
tree = E.Element('spriter_data', scml_version='1.0', generator='BaiyeCompactTransit', generator_version='1.0')
folder = E.SubElement(tree, 'folder', id='0')
files = [('body_0.png',256,512,0),('pole_glow_0.png',layout['pole']['width'],layout['pole']['height'],.5),
         ('flux_0.png',layout['field']['width'],layout['field']['height'],.5),
         ('arrow_up_0.png',layout['screen']['width'],layout['screen']['height'],.5),
         ('arrow_down_0.png',layout['screen']['width'],layout['screen']['height'],.5),
         ('meter_target_0.png',2,2,.5),('wax_meter_target_0.png',2,2,.5),('ui_0.png',128,128,.5)]
for kind in ['meter','wax_meter']:
    w,h=layout[kind]['width'],layout[kind]['height']
    files += [(f'{kind}_{i}.png',w,h,0) for i in range(51)]
for i,(name,w,h,pivot) in enumerate(files):
    E.SubElement(folder,'file',id=str(i),name=name,width=str(w),height=str(h),pivot_x='.5',pivot_y=str(pivot))
entity=E.SubElement(tree,'entity',id='0',name=NAME)

def add(name,frames=1,length=100,loop=False,direction=1,phase='idle'):
    anim=E.SubElement(entity,'animation',id=str(len(entity)),name=name,length=str(length),interval=str(length//frames),looping=str(loop).lower())
    main=E.SubElement(anim,'mainline')
    parts=['body_0','pole_glow_0','pole_glow_0','flux_0','flux_0',
           'arrow_up_0','arrow_down_0','meter_target_0','wax_meter_target_0']
    part_files=[0,1,1,2,2,3,4,5,6]
    for i in range(frames):
        k=E.SubElement(main,'key',id=str(i),time=str(i*length//frames))
        for j in range(len(parts)): E.SubElement(k,'object_ref',id=str(j),timeline=str(j),key=str(i),z_index=str(j))
    for j,symbol in enumerate(parts):
        tl=E.SubElement(anim,'timeline',id=str(j),name=symbol)
        for i in range(frames):
            p=i/max(1,frames-1)
            engaged=p if phase=='pre' else (1-p if phase=='post' else (1 if phase=='loop' else 0))
            attrs={'folder':'0','file':str(part_files[j]),'x':'0','y':'0','scale_x':'.390625','scale_y':'.390625'}
            if j == 0:
                body=layout['body']
                attrs.update(x=str(body['x']),y=str(body['y']),scale_x=str(body['width']/256),scale_y=str(body['height']/512))
            elif j in (1,2):
                pole=layout['pole']
                attrs.update(x=str((-1 if j==1 else 1)*pole['x']),y=str(pole['y']),a=str(.65*engaged))
            elif j in (3,4):
                field=layout['field']
                progress=p if phase=='pre' and direction else .5
                if direction<0: progress=1-progress
                attrs.update(x=str((-1 if j==3 else 1)*field['x']),
                             y=str(field['minY']+progress*(field['maxY']-field['minY'])),a=str(field['alpha']*engaged))
            elif j in (5,6):
                screen=layout['screen']
                active=phase=='pre' and direction==(1 if j==5 else -1)
                attrs.update(x=str(screen['x']),y=str(screen['upY' if j==5 else 'downY']),a=str(engaged if active else 0))
            elif j in (7,8):
                target=layout['meter' if j==7 else 'wax_meter']
                attrs.update(x=str(target['x']),y=str(target['y']),scale_x='1',scale_y='1')
            k=E.SubElement(tl,'key',id=str(i),time=str(i*length//frames))
            E.SubElement(k,'object',**attrs)

for name in ('off','on','idle','place'): add(name)
for name,direction in [('working_pre',0),('working_pre_up',1),('working_pre_down',-1),('wax',0),('wax_up',1),('wax_down',-1)]: add(name,8,400,direction=direction,phase='pre')
add('working_loop',12,600,True,phase='loop')
add('working_pst',8,400,phase='post',direction=0)
ui=E.SubElement(entity,'animation',id=str(len(entity)),name='ui',length='100',interval='100',looping='false')
main=E.SubElement(ui,'mainline'); key=E.SubElement(main,'key',id='0',time='0'); E.SubElement(key,'object_ref',id='0',timeline='0',key='0',z_index='0')
tl=E.SubElement(ui,'timeline',id='0',name='ui_0'); key=E.SubElement(tl,'key',id='0',time='0'); E.SubElement(key,'object',folder='0',file='7',x='0',y='0')
for kind,start in [('meter',8),('wax_meter',59)]:
    anim=E.SubElement(entity,'animation',id=str(len(entity)),name=kind,length='5100',interval='100',looping='false')
    main=E.SubElement(anim,'mainline')
    for i in range(51):
        key=E.SubElement(main,'key',id=str(i),time=str(i*100)); E.SubElement(key,'object_ref',id='0',timeline='0',key=str(i),z_index='0')
    tl=E.SubElement(anim,'timeline',id='0',name=kind+'_0')
    for i in range(51):
        key=E.SubElement(tl,'key',id=str(i),time=str(i*100)); E.SubElement(key,'object',folder='0',file=str(start+i),x='0',y='0',scale_x='.390625',scale_y='.390625')
E.indent(tree,space='  ')
E.ElementTree(tree).write(out/(NAME+'.scml'),encoding='utf-8',xml_declaration=True)
print('SCML:',len(files),'files,',len(entity),'animations')
