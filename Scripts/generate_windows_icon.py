from __future__ import annotations
from pathlib import Path
import math, sys
from PIL import Image, ImageDraw, ImageFilter

S = 1024
ICO_SIZES = [(256,256),(128,128),(64,64),(48,48),(40,40),(32,32),(24,24),(20,20),(16,16)]

def bez(p0,p1,p2,p3,n=80):
    out=[]
    for i in range(n+1):
        t=i/n; u=1-t
        out.append((u**3*p0[0]+3*u*u*t*p1[0]+3*u*t*t*p2[0]+t**3*p3[0],
                    u**3*p0[1]+3*u*u*t*p1[1]+3*u*t*t*p2[1]+t**3*p3[1]))
    return out

def icon_symbol(draw,kind,box):
    x0,y0,x1,y1=box; cx=(x0+x1)/2; cy=(y0+y1)/2; w=x1-x0; lw=7
    if kind=="globe":
        r=w*.27
        draw.ellipse((cx-r,cy-r,cx+r,cy+r),outline="white",width=lw)
        draw.line((cx-r,cy,cx+r,cy),fill="white",width=lw)
        draw.ellipse((cx-r*.45,cy-r,cx+r*.45,cy+r),outline="white",width=lw)
    elif kind=="mail":
        r=(x0+w*.16,y0+w*.25,x1-w*.16,y1-w*.25)
        draw.rounded_rectangle(r,radius=8,outline="white",width=lw)
        draw.line((r[0],r[1],cx,cy+5,r[2],r[1]),fill="white",width=lw)
    elif kind=="message":
        r=(x0+w*.15,y0+w*.22,x1-w*.15,y1-w*.28)
        draw.rounded_rectangle(r,radius=16,fill="white")
        draw.polygon([(r[0]+15,r[3]-3),(r[0]+8,r[3]+18),(r[0]+32,r[3]-3)],fill="white")
    elif kind=="folder":
        draw.rounded_rectangle((x0+w*.12,y0+w*.32,x1-w*.1,y1-w*.15),radius=10,fill="white")
        draw.rounded_rectangle((x0+w*.18,y0+w*.22,x0+w*.52,y0+w*.43),radius=8,fill="white")
    elif kind=="calendar":
        r=(x0+w*.16,y0+w*.18,x1-w*.16,y1-w*.13)
        draw.rounded_rectangle(r,radius=10,outline="white",width=lw)
        draw.line((r[0],r[1]+25,r[2],r[1]+25),fill="white",width=lw)

def card(kind,colors,w=470,h=145,angle=0):
    c=Image.new("RGBA",(w+100,h+100),(0,0,0,0))
    grad=Image.new("RGBA",(w,h)); gp=grad.load()
    for y in range(h):
        for x in range(w):
            t=(x+y)/(w+h); a,b=colors[0],colors[-1]
            gp[x,y]=tuple(int(a[k]*(1-t)+b[k]*t) for k in range(4))
    cm=Image.new("L",(w,h),0)
    ImageDraw.Draw(cm).rounded_rectangle((0,0,w-1,h-1),radius=38,fill=255)
    c.paste(grad,(50,50),cm)
    d=ImageDraw.Draw(c)
    d.rounded_rectangle((52,52,w+48,h+48),radius=36,outline=(255,255,255,110),width=4)
    tile=(78,72,174,168)
    d.rounded_rectangle(tile,radius=24,fill=(31,108,224,255))
    icon_symbol(d,kind,(96,90,156,150))
    d.rounded_rectangle((200,91,420,109),radius=9,fill=(213,234,255,200))
    d.rounded_rectangle((200,128,365,144),radius=8,fill=(181,215,255,143))
    return c.rotate(math.degrees(angle),resample=Image.Resampling.BICUBIC,expand=True)

def build_icon():
    img=Image.new("RGBA",(S,S),(0,0,0,0))
    shadow=Image.new("RGBA",(S,S),(0,0,0,0))
    ImageDraw.Draw(shadow).rounded_rectangle((28,28,996,996),radius=205,fill=(0,23,55,170))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(34)),(0,18))

    mask=Image.new("L",(S,S),0)
    ImageDraw.Draw(mask).rounded_rectangle((28,28,996,996),radius=205,fill=255)

    bg=Image.new("RGBA",(S,S))
    px=bg.load()
    stops=[(0.0,(4,38,92)),(.38,(7,91,193)),(.72,(105,84,213)),(1,(19,40,95))]
    for y in range(S):
        for x in range(S):
            t=(x+y)/(2*(S-1))
            for i in range(len(stops)-1):
                if stops[i][0] <= t <= stops[i+1][0]:
                    p,c1=stops[i]; q,c2=stops[i+1]
                    u=(t-p)/(q-p)
                    c=tuple(int(c1[k]*(1-u)+c2[k]*u) for k in range(3))
                    break
            px[x,y]=(*c,255)

    glow=Image.new("RGBA",(S,S),(0,0,0,0)); gd=ImageDraw.Draw(glow)
    for r,a in [(210,28),(160,55),(115,95),(78,145)]:
        gd.ellipse((780-r,190-r,780+r,190+r),fill=(255,173,64,a))
    bg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(24)))
    d=ImageDraw.Draw(bg)
    d.ellipse((718,136,842,260),fill=(255,248,191,255))
    d.polygon([(500,300),(610,225),(760,210),(900,250),(1000,286),(1000,515),(850,450),(690,470),(510,470)],fill=(23,31,103,245))

    road=[]
    road += bez((110,930),(245,920),(390,815),(535,710))[:-1]
    road += bez((535,710),(620,650),(640,585),(675,520))[:-1]
    road += bez((675,520),(700,465),(670,430),(610,410))[:-1]
    road += bez((610,410),(500,360),(635,300),(812,230))
    d.line(road,fill=(50,229,255,96),width=164,joint="curve")
    d.line(road,fill=(32,41,118,250),width=124,joint="curve")
    d.line(road,fill=(255,189,94,250),width=18,joint="curve")

    lengths=[0]
    for a,b in zip(road,road[1:]): lengths.append(lengths[-1]+math.dist(a,b))
    def point_at(dist):
        for i in range(1,len(lengths)):
            if lengths[i]>=dist:
                t=(dist-lengths[i-1])/(lengths[i]-lengths[i-1]); a,b=road[i-1],road[i]
                return (a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t)
        return road[-1]
    pos=0
    while pos<lengths[-1]:
        d.line((point_at(pos),point_at(min(lengths[-1],pos+46))),fill=(255,255,255,245),width=11)
        pos += 81

    specs=[
        ("globe",[(32,152,255,240),(134,90,232,184)],100,142,-.055),
        ("mail",[(42,143,255,240),(120,99,228,184)],70,265,-.04),
        ("message",[(26,205,195,235),(91,91,215,184)],58,390,.012),
        ("folder",[(255,182,63,240),(104,91,214,188)],70,515,.035),
        ("calendar",[(166,78,255,230),(42,131,217,184)],96,640,.052),
    ]
    for kind,colors,x,y,angle in specs:
        bg.alpha_composite(card(kind,colors,angle=angle),(x-50,y-50))

    d=ImageDraw.Draw(bg)
    d.ellipse((570,560,880,870),fill=(5,45,109,247),outline=(99,232,255,250),width=34)
    cx,cy=725,715
    d.line((cx,cy,cx,cy-78),fill="white",width=22)
    d.line((cx,cy,cx+70,cy+52),fill="white",width=22)
    d.arc((575,570,835,830),155,260,fill="white",width=24)
    d.polygon([(630,600),(642,550),(682,583)],fill="white")

    clipped=Image.new("RGBA",(S,S),(0,0,0,0))
    clipped.paste(bg,(0,0),mask)
    img.alpha_composite(clipped)
    ImageDraw.Draw(img).rounded_rectangle((31,31,993,993),radius=202,outline=(255,255,255,87),width=5)
    return img

def main():
    root=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
    assets=root/"Assets"; assets.mkdir(parents=True,exist_ok=True)
    image=build_icon()
    image.save(assets/"MemoryTrailIconMaster.png","PNG",optimize=True)
    image.resize((256,256),Image.Resampling.LANCZOS).save(assets/"MemoryTrailIcon.png","PNG",optimize=True)
    image.resize((512,512),Image.Resampling.LANCZOS).save(assets/"OnboardingIcon.png","PNG",optimize=True)
    image.save(assets/"MemoryTrailApp.ico","ICO",sizes=ICO_SIZES,bitmap_format="png")
    with Image.open(assets/"MemoryTrailApp.ico") as ico:
        available=set(ico.ico.sizes())
    missing=set(ICO_SIZES)-available
    if missing:
        raise RuntimeError(f"Missing ICO sizes: {sorted(missing)}")
    print("Generated Windows icon assets from the current macOS Memory Trail artwork specification.")

if __name__=="__main__":
    main()
