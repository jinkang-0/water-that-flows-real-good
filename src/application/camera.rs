
use linalg::{Vector, Matrix, transform_4x4};

/// Structure containing information about a perspective pinhole camera
pub struct Camera {
    // camera transformation
    // origin of the pinhole
    pub pos: Vector<f64, 3>,
    // pitch in radians ranging from -PI/2 to PI/2
    pub pitch: f64,
    // yaw in radians
    pub yaw: f64,

    // camera properties
    pub v_fov: f64,
    pub near: f64,
    pub far: f64,
    pub aspect: f64,
}
impl Camera {
    pub fn new(v_fov: f64, near: f64, far: f64) -> Self {
        Self {
            pos: [0.0, 0.0, 0.0].into(),
            pitch: 0.0,
            yaw: 0.0,

            v_fov,
            near,
            far,
            aspect: 1.0,
        }
    }
    /// Clamp the `pitch` to normal ranges (prevent the camera from going upside down)
    /// Translate `yaw` to the range [0,2PI] to maintain full precision when the camera is rotated a lot
    pub fn clamp_values(&mut self) {
        use std::f64::consts::PI;
        // clamp pitch
        if self.pitch < -0.5 * PI {
            self.pitch = -0.5 * PI;
        } else if self.pitch > 0.5 * PI {
            self.pitch = 0.5 * PI;
        }
        // move yaw
        self.yaw = self.yaw % (2.0*PI);
    }
    /// Calculate the camera view matrix
    pub fn view(&self) -> Matrix<f32, 4, 4> {
        // inverse of the camera transformation
        let rot_yaw_inv = transform_4x4::rotate_x(self.pitch);
        let rot_pitch_inv = transform_4x4::rotate_z(self.yaw);
        let translate_inv = transform_4x4::translate(-self.pos.x(), -self.pos.y(), -self.pos.z());
        (rot_yaw_inv * rot_pitch_inv * translate_inv).into()
    }
    /// Calculate camera perspective projection matrix
    pub fn proj(&self) -> Matrix<f32, 4, 4> {
        transform_4x4::perspective(self.v_fov, self.aspect, self.near, self.far).into()
    }
}

