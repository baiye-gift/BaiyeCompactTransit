"""Inspect source geometry, symbols and exported bytes; no image editing."""
from pathlib import Path
import re
import json
import xml.etree.ElementTree as E
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
NAME='baiye_compact_transit'
source=ROOT/'art/source'/NAME
assets=ROOT/'anim/assets'/NAME
layout=json.loads((ROOT/'art/station-layout.json').read_text(encoding='utf-8'))
tree=E.parse(source/(NAME+'.scml')).getroot()
files={f.attrib['id']:f.attrib for f in tree.findall('folder/file')}
anims={a.attrib['name']:a for a in tree.findall('entity/animation')}
needed={'off','on','idle','place','ui','working_pre','working_pre_up','working_pre_down','working_loop','working_pst','wax','wax_up','wax_down','meter','wax_meter'}
assert needed <= anims.keys()
for f in files.values():
    with Image.open(source/f['name']) as im:
        assert im.mode=='RGBA' and im.size==(int(f['width']),int(f['height'])),f['name']
for anim in anims.values():
    for tl in anim.findall('timeline'):
        for key in tl.findall('key'):
            obj=key.find('object');f=files[obj.attrib['file']]
            assert re.sub(r'_\d+$','',tl.attrib['name'])==re.sub(r'_\d+$','',Path(f['name']).stem)
for meter in ['meter','wax_meter']:
    keys=anims[meter].find('timeline').findall('key')
    assert len(keys)==51
    with Image.open(source/files[keys[-1].find('object').attrib['file']]['name']) as im:
        assert im.size==(layout[meter]['width'],layout[meter]['height'])
    for idx,visible in [(0,False),(-1,True)]:
        name=files[keys[idx].find('object').attrib['file']]['name']
        with Image.open(source/name) as im: assert bool(im.getchannel('A').getbbox())==visible
for name in needed-{'ui','meter','wax_meter'}:
    targets={t.attrib['name'] for t in anims[name].findall('timeline')}
    assert {'body_0','meter_target_0','wax_meter_target_0'}<=targets
    for obj in anims[name].findall('timeline/key/object'):
        if obj.attrib['file']=='0':
            assert float(obj.attrib['scale_x'])*256==layout['body']['width']
            assert float(obj.attrib['scale_y'])*512==layout['body']['height']
            assert float(obj.attrib['y'])+layout['body']['height']==200
# One structural body owns the open pipe walls, magnets and floor. Light-only
# symbols cannot introduce an independent complete tube behind the station.
allowed={'body_0','pole_glow_0','arrow_up_0','arrow_down_0','flux_0','meter_target_0','wax_meter_target_0'}
for name in needed-{'ui','meter','wax_meter'}:
    timelines=anims[name].findall('timeline')
    assert sum(t.attrib['name']=='body_0' for t in timelines)==1,'duplicate structural body'
    assert {t.attrib['name'] for t in timelines}==allowed,'unexpected structural overlay'
    for timeline in timelines:
        if timeline.attrib['name']!='flux_0': continue
        for key in timeline.findall('key'):
            obj=key.find('object'); half_h=layout['field']['height']*.390625/2
            assert half_h<float(obj.attrib['y'])<200-half_h,'field escapes the station'
            assert abs(float(obj.attrib['x']))==layout['field']['x']>=40,'field crosses occupant center'
            assert layout['field']['width']*.390625<3,'field is too wide'
            assert 0<=float(obj.attrib['a'])<=layout['field']['alpha'],'opaque magnetic field'
for name,sign in [('working_pre_up',1),('working_pre_down',-1)]:
    ring=next(t for t in anims[name].findall('timeline') if t.attrib['name']=='flux_0')
    positions=[float(k.find('object').attrib['y']) for k in ring.findall('key')]
    assert len(set(positions))>=6,'stationary directional field'
    assert all(sign*(b-a)>0 for a,b in zip(positions,positions[1:])),'field indicates the wrong direction'
# Idle has no active field over the occupant. The integrated body's center is
# actual alpha, not the face of a closed opaque glass cylinder.
for name in ['on','off','idle','place']:
    for tl in anims[name].findall('timeline')[1:7]:
        assert float(tl.find('key/object').attrib['a'])==0,name
with Image.open(source/'body_0.png') as im:
    alpha=im.getchannel('A')
    walk=[alpha.getpixel((x,y)) for y in range(160,370) for x in range(20,236)]
    assert sum(a<24 for a in walk)/len(walk)>.65,'central walking region is visually closed'
    # An unobstructed center continues to the thin bottom deck.
    assert all(alpha.getpixel((128,y))<24 for y in range(200,440)),'separate closed pipe or central mast'
    assert alpha.getpixel((128,495))>24,'standing platform is missing'
    # Deck surface is level with the native floor; its lower collar can overhang.
    assert abs(layout['body']['y']+layout['body']['height']*(1-445/512))<6,'deck surface misses the floor'
for name in needed-{'ui','meter','wax_meter'}:
    for symbol,sign in [('arrow_up_0',1),('arrow_down_0',-1)]:
        timeline=next(t for t in anims[name].findall('timeline') if t.attrib['name']==symbol)
        lit=any(float(k.find('object').attrib['a'])>0 for k in timeline.findall('key'))
        assert lit==(name in (['working_pre_up','wax_up'] if sign==1 else ['working_pre_down','wax_down'])),name
for meter in ['meter','wax_meter']:
    target='meter_target_0' if meter=='meter' else 'wax_meter_target_0'
    obj=next(t for t in anims['on'].findall('timeline') if t.attrib['name']==target).find('key/object')
    assert float(obj.attrib['x'])==layout[meter]['x'] and float(obj.attrib['y'])==layout[meter]['y']
with Image.open(source/'ui_0.png') as im: assert im.size==(128,128)
anim_bytes=(assets/(NAME+'_anim.bytes')).read_bytes()
build_bytes=(assets/(NAME+'_build.bytes')).read_bytes()
assert anim_bytes[:4]==b'ANIM' and build_bytes[:4]==b'BILD'
for name in needed: assert name.encode() in anim_bytes,name
with Image.open(next(assets.glob('*.png'))) as im: assert max(im.size)<=2048
print(f'PASS: {len(anims)} animations, {len(files)} sprites, one integrated open body, logical 1x2, transparent center, edge flow and direction screen, clear idle, 128px icon, two 51-frame meters, valid KAnim and bounded atlas')
