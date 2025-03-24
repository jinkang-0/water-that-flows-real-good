
use linalg::Vector;

/// Simple timer structure for keeping track of frame timing
pub struct FrameTimer {
    t_start: f64,
    t_end: f64,
}
impl FrameTimer {
    pub fn new() -> Self { Self { t_start: 0.0, t_end: 0.0, } }
    /// Mark the start of the frame
    /// returns the time for the previous frame
    pub fn start_frame(&mut self, current_time: f64) -> f64 {
        self.t_end = self.t_start;
        self.t_start = current_time;
        self.t_start - self.t_end
    }
}

/// Simple structure for keeping track of cursor movement
pub struct CursorMotionTracker {
    reset_tag: bool,
    // keep track of the position before and after the events of the frame
    // position at start of frame
    start_x: f64,
    start_y: f64,
    // position at end of frame
    end_x: f64,
    end_y: f64,
}
impl CursorMotionTracker {
    pub fn new() -> Self {
        Self {
            reset_tag: false,
            start_x: 0.0,
            start_y: 0.0,
            end_x: 0.0,
            end_y: 0.0,
        }
    }
    pub fn start_capture(&mut self) {
            self.start_x = self.end_x;
            self.start_y = self.end_y;
    }
    /// Get the final change in position of the mouse cursor
    pub fn get_motion(&self) -> Vector<f64, 2> {
            [self.end_x - self.start_x, self.end_y - self.start_y]
        .into()
    }
    /// Update the tracker with a cursor position event
    pub fn cursor_pos_event(&mut self, x: f64, y: f64) {
            if self.reset_tag {
                // the previous positions before the reset tag should be ignored
                // count the first position event as the start of the motion
                self.start_x = x;
                self.start_y = y;
                self.end_x = x;
                self.end_y = y;
                self.reset_tag = false;
            } else {
                // normal event (was not just tagged)
                // update the x and y end positions to the most recent event
                self.end_x = x;
                self.end_y = y;
            }
    }
    /// if the cursor leaves the window, when it returns, it should use the next position event as the start of the motion
    /// this method signals that the next position event should be used as the starting position
    pub fn tag_reset(&mut self) {
        self.reset_tag = true;
    }
}

/// A free-fly first person camera controller
/// Keeps information about the camera controls (sensitivity and movement speed)
pub struct CameraControl {
    pub sensitivity_pitch: f64,
    pub sensitivity_yaw: f64,
    pub normal_speed_xy: f64,
    pub normal_speed_z: f64,
    pub sprint_speed_xy: f64,
    pub sprint_speed_z: f64,
}
impl Default for CameraControl {
    fn default() -> Self {
        Self {
            sensitivity_pitch: 0.0015,
            sensitivity_yaw: 0.0015,
            normal_speed_xy: 1.0,
            normal_speed_z: 1.0,
            sprint_speed_xy: 2.0,
            sprint_speed_z: 2.0,
        }
    }
}
impl CameraControl {
    pub fn handle_camera_control_input(&self, main_window: &glfw::PWindow, camera: &mut super::camera::Camera, dt: f64, cursor_movement: Vector<f64, 2>) {
        use glfw::{Key, Action};

        // rotate the camera based on cursor movement
        camera.yaw += self.sensitivity_yaw * cursor_movement.x();
        camera.pitch += self.sensitivity_pitch * cursor_movement.y();
        // prevent the camera from facing upside down
        camera.clamp_values();

        // get key presses and move the camera position accordingly
        let mut movxy = Vector::<f64, 2>::from([0.0, 0.0]);
        let mut movz = 0.0;
        if main_window.get_key(Key::W) == Action::Press {
            *movxy.y_mut() += 1.0;
        }
        if main_window.get_key(Key::S) == Action::Press {
            *movxy.y_mut() -= 1.0;
        }
        if main_window.get_key(Key::D) == Action::Press {
            *movxy.x_mut() += 1.0;
        }
        if main_window.get_key(Key::A) == Action::Press {
            *movxy.x_mut() -= 1.0;
        }
        if main_window.get_key(Key::Space) == Action::Press {
            movz += 1.0;
        }
        if main_window.get_key(Key::C) == Action::Press {
            movz -= 1.0;
        }
        let shift_is_pressed = main_window.get_key(Key::LeftShift) == Action::Press;

        movz *= if shift_is_pressed { self.sprint_speed_z } else { self.normal_speed_z };
        if movxy != Vector::<f64, 2>::from([0.0, 0.0]) {
            // normalize the horizontal movement (so moving diagonal is not faster)
            movxy /= movxy.norm();
            // movement speed
            movxy *= if shift_is_pressed { self.sprint_speed_xy } else { self.normal_speed_xy };
            // move in the direction the camera is facing
            movxy = linalg::transform_2x2::rotate(-camera.yaw) * movxy;
        }
        // update the camera position
        // normalize with delta time `dt` (this way the camera moves X amount per time rather than X amount per frame)
        camera.pos += Vector::<f64, 3>::from([movxy.x(), movxy.y(), movz]) * dt;
    }
}

