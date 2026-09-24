import numpy as np, math, os
from scipy import ndimage as ndi
from PIL import Image, ImageDraw, ImageFilter, ImageFont
P=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
source=Image.open(P+'/Assets/BoxSorterUFO2D/Textures/UFO_Original.jpeg').convert('RGB')
arr=np.array(source).astype(np.float32)/255
h,w=arr.shape[:2]; y,x=np.mgrid[:h,:w]
def smoothstep(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
def lens(cx,cy,rx,ry,angle):
 qx=x-cx;qy=y-cy;cs=math.cos(angle);sn=math.sin(angle)
 r=np.sqrt(((cs*qx+sn*qy)/rx)**2+((-sn*qx+cs*qy)/ry)**2)
 return 1-smoothstep(.88,1.04,r)
masks=[lens(255,951,45,55,-.57),lens(598,1021,61,49,0),lens(936,954,45,57,.57)]
# A separate silhouette mask preserves white reflections inside the UFO.
labels,nlabels=ndi.label(arr.min(2)<.90)
areas=np.bincount(labels.ravel());areas[0]=0
silhouette=labels==areas.argmax()
silhouette=ndi.binary_fill_holes(silhouette)
silhouette=ndi.binary_closing(silhouette,iterations=2)
alpha=ndi.gaussian_filter(silhouette.astype(float),.55)
Image.fromarray(np.round(alpha*255).astype('uint8')).save(P+'/Assets/BoxSorterUFO2D/Textures/UFO_SilhouetteMask.png')
ufos=[]
for count in range(4):
 dim=np.maximum.reduce(masks[count:]) if count<3 else np.zeros((h,w))
 rgb=arr*(1-.84*dim[...,None])
 rgba=np.dstack([rgb,alpha]);ufos.append(Image.fromarray(np.round(rgba*255).astype('uint8')).crop((110,390,1095,1090)))
# At state 3, every opaque source RGB pixel is unchanged.
assert np.array_equal(np.array(ufos[3])[:,:,:3],np.array(source)[390:1090,110:1095])
# Preview of OriginalSprite.shader using the unchanged source JPEG.
S=(540,760);frames=[]; fps=24;duration=5
fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
font=ImageFont.truetype(fontpath,15);bold=ImageFont.truetype(fontpath,23)
def smooth(x):x=max(0,min(1,x));return x*x*(3-2*x)
def apple(im,x,y,s):
 if s<4:return
 d=ImageDraw.Draw(im);r=s/2
 d.ellipse((x-r,y-r*.75,x+r,y+r),fill=(210,34,51,255));d.ellipse((x-r*.75,y-r*.70,x+r*.55,y+r*.64),fill=(240,55,65,255));d.ellipse((x-r*.48,y-r*.45,x-r*.12,y-r*.13),fill=(255,158,147,255));d.line((x,y-r*.65,x+2,y-r*1.05),fill=(103,65,28),width=max(2,int(s*.06)));d.ellipse((x+1,y-r*1.1,x+r*.62,y-r*.67),fill=(99,204,63,255))
for frame in range(int(fps*duration)):
 t=frame/fps;im=Image.new('RGBA',S,(25,27,35,255));d=ImageDraw.Draw(im)
 d.text((30,25),'ORIGINAL UFO / 2.5D',font=bold,fill=(242,242,250));d.text((30,58),'BOX SORTER  /  ANIMATION PREVIEW',font=font,fill=(148,153,175))
 d.rounded_rectangle((22,100,518,726),35,fill=(45,48,58),outline=(73,78,93),width=3)
 d.rounded_rectangle((43,122,497,651),26,fill=(24,26,34))
 enter=smooth(t/.65);leave=smooth((t-3.65)/.7);ux=270+360*(1-enter)+360*leave;uy=234-170*(1-enter)-140*leave+math.sin(t*5)*3
 alpha=smooth((t-.60)/.25)*(1-smooth((t-3.35)/.25))
 if alpha>0:
  glow=Image.new('RGBA',S);gd=ImageDraw.Draw(glow);gd.polygon([(ux-15,uy+35),(ux+15,uy+35),(420,581),(120,581)],fill=(25,194,255,int(32*alpha)));gd.ellipse((120,559,420,601),fill=(30,210,255,int(54*alpha)))
  for k in range(5):
   q=((t*.75+k/5)%1);yy=580-q*300;rr=140*(1-q)+12
   gd.ellipse((270-rr,yy-rr*.16,270+rr,yy+rr*.16),outline=(82,226,255,int(alpha*180*(1-q))),width=2)
  im=Image.alpha_composite(im,glow.filter(ImageFilter.GaussianBlur(4)));im=Image.alpha_composite(im,glow)
 for i in range(3):
  start=.95+i*.63;q=max(0,min(1,(t-start)/.72));e=smooth(q);x0=[168,275,379][i];y0=[550,597,534][i]
  if q<1:apple(im,x0*(1-e)+ux*e+math.sin(q*math.pi*2)*18*math.sin(q*math.pi),y0*(1-e)+(uy+35)*e,57*(1-e)**.6)
 count=sum(t>=.95+i*.63+.72 for i in range(3))
 sprite=ufos[count].resize((370,263),Image.Resampling.LANCZOS);im.alpha_composite(sprite,(round(ux-185),round(uy-131)))
 d=ImageDraw.Draw(im);complete=t>=2.93
 d.rounded_rectangle((225,655,315,714),8,fill=(188,111,45),outline=(244,172,86),width=3)
 d.text((246,675),'3/3' if complete else '0/3',font=font,fill='white')
 if complete:d.rounded_rectangle((219,651,321,663),4,fill=(244,174,83));d.text((335,677),'COMPLETE',font=font,fill=(107,233,177))
 frames.append(im.convert('RGB'))
frames[78].save(P+'/Preview_Still.jpg')
# Four capture states for visual QA.
sheet=Image.new('RGB',(1080,1520))
for k,f in enumerate([25,44,59,77]):sheet.paste(frames[f],((k%2)*540,(k//2)*760))
sheet.save(P+'/Lamp_Sequence_Check.jpg')
frames[0].save(P+'/UFO_Booster_Preview.gif',save_all=True,append_images=frames[1:],duration=round(1000/fps),loop=0,optimize=False)
os.makedirs(P+'/frames',exist_ok=True)
for i,im in enumerate(frames):im.save(P+f'/frames/{i:04d}.png')
print('Original RGB verified; preview frames:',len(frames))
