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

	static public void UpdateFluid(
		int N,
		float[][] horSpeed, float[][] verSpeed, float[][] dens, float[][] verGrad, float[][] horGrad,
		float[][] prevHorGrad, float[][] prevVerGrad, float[][] horSpeedSource, float[][] verSpeedSource, float[][] densSource,
		float flui, float diff,
		float dt
		) {
		Gradient (N, verGrad, horGrad, prevHorGrad, prevVerGrad, dens); //

		int i, j;
		for (i=1 ; i<=N ; ++i ) { 
			for (j=1 ; j<=N ; ++j ) {
				horSpeed[i][j] = horGrad[i][j];
				verSpeed[i][j] = verGrad[i][j];
			} 
		}

		VelocityStep (N, horSpeed, verSpeed, horSpeedSource, verSpeedSource, flui, dt);

		DensityStep (N, dens, densSource, horSpeed, verSpeed, diff, dt);
		FixSpeed (N, horSpeed, verSpeed, dens);

		//Gradient (N, verGrad, horGrad, prevHorGrad, prevVerGrad, dens);
	}
	
	static void VelocityStep (int N, float[][] u, float[][] v, float[][] u0, float[][] v0, float flui, float dt ) { 
		if (u0 != null)	AddSource ( N, u, u0, dt );
		if (v0 != null) AddSource ( N, v, v0, dt );

		Swap (ref /*u0*/temp, ref u );
		Diffuse ( N, 1, u, /*u0*/temp, flui, dt ); 

		Swap (ref /*v0*/temp2, ref v );
		Diffuse ( N, 2, v, /*v0*/temp2, flui, dt );

		Project ( N, u, v, /*u0*/temp, /*v0*/temp2 );

		Swap (ref /*u0*/temp, ref u );
		Swap (ref /*v0*/temp2, ref v );

		Advect ( N, 1, u, /*u0*/temp, /*u0*/temp, /*v0*/temp2, dt );
		Advect ( N, 2, v, /*v0*/temp2, /*u0*/temp, /*v0*/temp2, dt );

		Project ( N, u, v, /*u0*/temp, /*v0*/temp2 );
	}

	static void DensityStep (int N, float[][] x, float[][] x0, float[][] u, float[][] v, float diff, float dt ) {
		if (x0 != null)	AddSource ( N, x, x0, dt );
		
		Swap (ref /*x0*/ temp, ref x );
		Diffuse ( N, 0, x, /*x0*/temp, diff, dt );
		
		Swap (ref /*x0*/temp, ref x );
		Advect ( N, 0, x, /*x0*/temp, u, v, dt ); 
	}

	static void Gradient ( int N, float[][] verGrad, float[][] horGrad, float[][] prevVerGrad, float[][] prevHorGrad, float[][] dens) {
		float h = 1.0f / N / 2;
		float pow = .1f;
		
		int i, j;
		if (prevVerGrad != null && prevHorGrad != null)
		{
			for (i=1; i<=N; ++i)
			{ 
				for (j=1; j<=N; ++j)
				{
					verGrad [i] [j] =
						prevVerGrad [i] [j] +
							pow * (
								dens [i] [j - 1] -
								dens [i] [j + 1]
							);

					horGrad [i] [j] =
						prevHorGrad [i] [j] +
							pow * (
								dens [i - 1] [j] -
								dens [i + 1] [j]
							);
				} 
			}
		}
		else {
			for (i=1; i<=N; ++i)
			{ 
				for (j=1; j<=N; ++j)
				{
					verGrad [i] [j] =
						pow * (
							dens [i] [j - 1] -
							dens [i] [j + 1]
							);
					
					horGrad [i] [j] =
						pow * (
							dens [i - 1] [j] -
							dens [i + 1] [j]
							);
				} 
			}
		}
	}

	static void Diffuse ( int N, int b, float[][] x, float[][] x0, float diff, float dt ) {
		float a=dt*diff*N*N;
		
		int k, i, j;
		for (k=0 ; k<20 ; ++k ) {
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
			SetBound ( N, b, x );
		}
	}

	static void Advect ( int N, int b, float[][] d, float[][] d0, float[][] u, float[][] v, float dt ) { 
		int i0, j0, i1, j1; 
		float x, y, s0, t0, s1, t1, dt0;

		dt0 = dt*N;

		int i, j;
		for (i=1; i<=N; i++) { 
			for (j=1; j<=N; j++) {
				d[i][j] = 0;
			}
		}
		for (i=1 ; i<=N ; i++ ) { 
			for (j=1 ; j<=N ; j++ ) {
				x = i+dt0*u[i][j];
				y = j+dt0*v[i][j]; 

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

				d[i0][j0] += d0[i][j]*s0*t0;
				d[i0][j1] += d0[i][j]*s0*t1;
				d[i1][j0] += d0[i][j]*s1*t0;
				d[i1][j1] += d0[i][j]*s1*t1;

				/*x = i-dt0*u[i][j];
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
				
				d[i][j] = 
					s0*(
						t0*d0[i0][j0] +
						t1*d0[i0][j1]
						)
						+
					s1*(
						t0*d0[i1][j0] +
						t1*d0[i1][j1]
						);*/
			} 
		} 
		SetBound ( N, b, d );
	}


	//
	// Helpers
	static void AddSource ( int N, float[][] x, float[][] s, float dt ) {
		int i, j;
		for (i=0 ; i<N+2 ; ++i ) { 
			for (j=0 ; j<N+2 ; ++j ) { 
				x [i][j] += dt * s [i][j];
			}
		}
	}

	static void FixSpeed ( int N, float[][] horSpeed, float[][] verSpeed, float[][] dens) {
		int i, j;
		for (i=0 ; i<N+2 ; ++i ) { 
			for (j=0 ; j<N+2 ; ++j ) {
				if (dens[i][j] < 0.001) {
					float mult = dens[i][j] * 1000.0f;
					horSpeed[i][j] *= mult;
					verSpeed[i][j] *= mult;
				}
			}
		}
	}

	static void Project ( int N, float[][] u, float[][] v, float[][] p, float[][] div ) {
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
