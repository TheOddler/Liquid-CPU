using UnityEngine;
using System.Collections;
using System.Linq;

// Paper used: http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf

public static class FluidSolver {
	public const int N = 100;
	public const int size=(N+2)*(N+2);

	static float[][]
		temp = new float[N+2][],
		temp2 = new float[N+2][];

	// Use this for initialization
	static FluidSolver() {
		// Initialize all jagged arrays
		for (int i = 0; i < N+2; ++i) {
			temp[i] = new float[N+2];
			temp2[i] = new float[N+2];
		}
	}

	static public void UpdateFluid(int N, float[][] horSpeed, float[][] verSpeed, float[][] dens, float[][] horSpeedSource, float[][] verSpeedSource, float[][] densSource, float flui, float diff, float dt) {
		VelocityStep (N, horSpeed, verSpeed, horSpeedSource, verSpeedSource, flui, dt);
		DensityStep (N, dens, densSource, horSpeed, verSpeed, diff, dt);
	}
	
	static void DensityStep (int N, float[][] x, float[][] x0, float[][] u, float[][] v, float diff, float dt ) {
		//_extraTimer.Start (); 8.6

		//_extraTimer.Start (); 0.07
		AddSource ( N, x, x0, dt );
		//_extraTimer.Stop ();

		Swap (ref /*x0*/ temp, ref x );
		//_extraTimer.Start (); //7.8
		Diffuse ( N, 0, x, /*x0*/temp, diff, dt );
		//_extraTimer.Stop ();

		Swap (ref /*x0*/temp, ref x );
		//_extraTimer.Start (); //0.66
		Advect ( N, 0, x, /*x0*/temp, u, v, dt ); 
		//_extraTimer.Stop ();

		//_extraTimer.Stop ();
	}

	
	static void VelocityStep (int N, float[][] u, float[][] v, float[][] u0, float[][] v0, float flui, float dt ) { 
		//_extraTimer.Start (); //33.2

		//_extraTimer.Start (); 0.14
		AddSource ( N, u, u0, dt );
		AddSource ( N, v, v0, dt );
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //15.4
		Swap (ref /*u0*/temp, ref u );
		Diffuse ( N, 1, u, /*u0*/temp, flui, dt ); 

		Swap (ref /*v0*/temp2, ref v );
		Diffuse ( N, 2, v, /*v0*/temp2, flui, dt );
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //7.9
		project ( N, u, v, /*u0*/temp, /*v0*/temp2 ); 
		//_extraTimer.Stop ();

		Swap (ref /*u0*/temp, ref u );
		Swap (ref /*v0*/temp2, ref v );

		//_extraTimer.Start (); //1.3
		Advect ( N, 1, u, /*u0*/temp, /*u0*/temp, /*v0*/temp2, dt );
		Advect ( N, 2, v, /*v0*/temp2, /*u0*/temp, /*v0*/temp2, dt ); 
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //8
		project ( N, u, v, /*u0*/temp, /*v0*/temp2 ); 
		//_extraTimer.Stop ();

		//_extraTimer.Stop ();
	} 

	/*static void DiffuseDensity ( int N, int b, float[][] x, float[][] x0, float diff, float dt ) {
		//_diffuseTimer.Start (); //7.8 ; 7.2 ; 
		int k, i, j;
		float a=dt*diff*N*N;

		//bounds
		for (i=1 ; i<=N ; ++i ) { 
			x[0][i]		= (	x0[0][i] +		a*(	x[0][i-1] +		x[0][i+1] +		x[1][i]	))		/(1+3*a);
			x[N+1][i]	= (	x0[N+1][i] +	a*(	x[N+1][i-1] +	x[N+1][i+1] +	x[N][i] ))		/(1+3*a);
			x[i][0]		= (	x0[i][0] +		a*( x[i+1][0] +		x[i-1][0] +		x[i][1] ))		/(1+3*a);
			x[i][N+1]	= (	x0[i][N+1] +	a*(	x[i-1][N+1] +	x[i+1][N+1] +	x[i][N]	))		/(1+3*a);
		}
		x[0][0]		= (x0[0][0]		+ a*(	x[1][0] 	+	x[0][1]		))	/(1+2*a); 
		x[0][N+1]	= (x0[0][N+1]	+ a*(	x[1][N+1]	+	x[0][N]		))	/(1+2*a); 
		x[N+1][0]	= (x0[N+1][0]	+ a*(	x[N][0]		+	x[N+1][1]	))	/(1+2*a);
		x[N+1][N+1]	= (x0[N+1][N+1]	+ a*(	x[N][N+1]	+	x[N+1][N]	))	/(1+2*a);
		
		//middle
		for (k=0 ; k<20 ; ++k ) {
			//_extraTimer.Start(); 0.3
			//SetBound ( N, b, x );

			for (i=1 ; i<=N ; ++i ) { 
				for (j=1 ; j<=N ; ++j ) { 
					x[i][j] =
						(
							x0[i][j] +
							a*(
							x[i-1][j] +
							x[i+1][j] +
							x[i][j-1] +
							x[i][j+1]
							)
						)/(1+4*a); 
				} 
			}
			//_extraTimer.Stop();

			//SetBound ( N, b, x );
		}

		//_diffuseTimer.Stop ();
	}*/

	static void Diffuse ( int N, int b, float[][] x, float[][] x0, float diff, float dt ) {
		float a=dt*diff*N*N;
		
		int k, i, j;
		for (k=0 ; k<20 ; ++k ) {
			//_extraTimer.Start(); 0.3
			//SetBound ( N, b, x );

			for (i=1 ; i<=N ; ++i ) { 
				for (j=1 ; j<=N ; ++j ) { 
					x[i][j] =
						(
							x0[i][j] +
							a*(
							x[i-1][j] +
							x[i+1][j] +
							x[i][j-1] +
							x[i][j+1]
							)
						)/(1+4*a); 
				} 
			}
			//_extraTimer.Stop();

			SetBound ( N, b, x );

		}
	}

	static void Advect ( int N, int b, float[][] d, float[][] d0, float[][] u, float[][] v, float dt ) { 
		int i0, j0, i1, j1; 
		float x, y, s0, t0, s1, t1, dt0;

		dt0 = dt*N;

		int i, j;
		for (i=1 ; i<=N ; i++ ) { 
			for (j=1 ; j<=N ; j++ ) {
				x = i-dt0*u[i][j];
				y = j-dt0*v[i][j]; 

				if (x<0.5f) x= 0.5f;
				if (x>N+0.5f) x= N+0.5f;
				i0=(int)x;
				i1=i0+1;

				if (y<0.5f) y= 0.5f;
				if (y>N+0.5f) y= N+0.5f;
				j0=(int)y;
				j1=j0+1;

				s1 = x-i0;
				s0 = 1-s1;
				t1 = y-j0;
				t0 = 1-t1;

				//float dOri = d[i][j];
				d[i][j] = 
					s0*(
						t0*d0[i0][j0] +
						t1*d0[i0][j1]
						)
						+
					s1*(
						t0*d0[i1][j0] +
						t1*d0[i1][j1]
						);
				/*if (dOri != d[i][j]) {
					Debug.Log("d changed: " + (dOri - d[i][j]));
				}*/
			} 
		} 
		SetBound ( N, b, d );
	}


	//
	// Helpers
	static void AddSource ( int N, float[][] x, float[][] s, float dt ) { 
		//int size=(N+2)*(N+2);
		int i, j;
		for (i=0 ; i<N+2 ; ++i ) { 
			for (j=0 ; j<N+2 ; ++j ) { 
				x [i][j] += dt * s [i][j];
			}
		}
	} 

	static void project ( int N, float[][] u, float[][] v, float[][] p, float[][] div ) {
		int k, i, j;
		float h;
		h = 1.0f/N; 
		for (i=1 ; i<=N ; ++i ) { 
			for (j=1 ; j<=N ; ++j ) { 
				div[i][j] = -0.5f*h*(u[i+1][j]-u[i-1][j]+v[i][j+1]-v[i][j-1]); 
				p[i][j] = 0; 
			} 
		} 
		SetBound ( N, 0, div ); SetBound ( N, 0, p ); 
		
		for (k=0 ; k<20 ; ++k ) { 
			for (i=1 ; i<=N ; ++i ) { 
				for (j=1 ; j<=N ; ++j ) { 
					p[i][j] = (div[i][j]+p[i-1][j]+p[i+1][j]+ p[i][j-1]+p[i][j+1])/4; 
				} 
			} 
			SetBound ( N, 0, p ); 
		} 
		
		for (i=1 ; i<=N ; i++ ) { 
			for (j=1 ; j<=N ; j++ ) { 
				u[i][j] -= 0.5f*(p[i+1][j]-p[i-1][j])/h; 
				v[i][j] -= 0.5f*(p[i][j+1]-p[i][j-1])/h; 
			} 
		} 
		SetBound ( N, 1, u ); SetBound ( N, 2, v ); 
	}
	
	static void SetBound ( int N, int b, float[][] x ) {
		for (int i=1 ; i<=N ; i++ ) { 
			x[0][i]		= 0; //b==1 ? -x[1][i] : x[1][i];
			x[N+1][i]	= 0; //b==1 ? -x[N][i] : x[N][i];
			x[i][0]		= 0; //b==2 ? -x[i][1] : x[i][1];
			x[i][N+1]	= 0; //b==2 ? -x[i][N] : x[i][N];
		}
		x[0][0]		= 0; //0.5f*(	x[1][0]		+	x[0][1]		); 
		x[0][N+1]	= 0; //0.5f*(	x[1][N+1]	+	x[0][N]		); 
		x[N+1][0]	= 0; //0.5f*(	x[N][0]		+	x[N+1][1]	); 
		x[N+1][N+1]	= 0; //0.5f*(	x[N][N+1]	+	x[N+1][N]	);
	} 

	public static int CalculateIndex(int i, int j) {
		return ((i) + (N + 2) * (j));
	}

	static void Swap(ref float[][] x0, ref float[][] x) {
		var tmp=x0;
		x0=x;
		x=tmp;
	} 
}
