export default function MainBackround ()
{
  return(
    <div className="absolute inset-0">
        <div className="relative h-full w-full bg-slate-950
          [&>div]:absolute
          [&>div]:bottom-0
          [&>div]:right-[-20%]
          [&>div]:top-[-10%]
          [&>div]:h-[500px]
          [&>div]:w-[500px]
          [&>div]:rounded-full
          [&>div]:bg-[radial-gradient(circle_farthest-side,rgba(0,245,200,0.18),rgba(255,255,255,0))]">
          
          <div></div>
        </div>
      </div>
  );
}
