package brandon.fsx;

import android.content.Context;
import android.graphics.Canvas;
import android.util.AttributeSet;

import com.google.android.maps.MapView;
import com.google.android.maps.Projection;

public class RotatedMapView extends MapView{
	private Projection projection;
	//private RotatedProjection rotatedProjection;
	private float scaleFactor;
	private float heading = 0.0f;



	public RotatedMapView(Context context, AttributeSet attrs) {
		super(context, attrs);
		projection = super.getProjection();
		//rotatedProjection = new RotatedProjection(projection, getWidth(), getHeight(), orientation, scaleFactor);
	}

	@Override
	protected void dispatchDraw(Canvas canvas) {
		canvas.save(Canvas.MATRIX_SAVE_FLAG);

		int w = this.getWidth();
		int h = this.getHeight();

		setScaleFactor((float)(Math.sqrt(h * h + w * w) / Math.min(w, h)));
		canvas.scale(scaleFactor, scaleFactor,w * 0.5f, h * 0.5f);
		
		canvas.rotate(-heading, w * 0.5f, h * 0.5f);        

		super.onDraw(canvas);


		canvas.restore();
	}

	private void setScaleFactor(float scaleFactor) {
		this.scaleFactor = scaleFactor;
		//rotatedProjection.setScaleFactor(scaleFactor);
	}

	public void setHeading(float heading) {
		this.heading = heading;
		//rotatedProjection.setOrientation(orientation);
		invalidate();
	}

	public float getHeading(){
		return this.heading;
	}

	public float getScaleFactor() {
		return scaleFactor;
	}



}