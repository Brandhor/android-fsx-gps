package brandon.fsx;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Set;

import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Point;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;

import com.google.android.maps.GeoPoint;
import com.google.android.maps.ItemizedOverlay;
import com.google.android.maps.MapView;
import com.google.android.maps.Overlay;
import com.google.android.maps.OverlayItem;


public class PlaneOverlay extends Overlay {
	Drawable drawable;
	Float heading = 0.0f;
	GeoPoint gp;
	
	public PlaneOverlay(Drawable drawable, GeoPoint gp, Float heading) {
		this.drawable = drawable;
		this.gp = gp;
		this.heading = heading;
	}
	
	@Override
	public void draw(Canvas canvas, MapView mapview, boolean shadow) {
		Paint paint = new Paint();
		Point pt  = new Point();
		mapview.getProjection().toPixels(this.gp, pt);
		
		Bitmap bmp = ((BitmapDrawable)drawable).getBitmap();
		int width = bmp.getWidth();
		int height = bmp.getHeight();
		
		Matrix matrix = new Matrix();
		matrix.postRotate(heading, width*0.5f, height*0.5f);
		matrix.postTranslate(pt.x-(width/2), pt.y-(height/2));
		paint.setAntiAlias(true);
		paint.setFilterBitmap(true);

		canvas.drawBitmap(bmp, matrix, paint);
	}

}
