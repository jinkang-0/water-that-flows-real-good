
mod application;

fn main() {
    {
        let mut app = application::Application::new(600, 400);
        println!("Application initialized");
        app.run();
        println!("Exiting...");
    }
    println!("Application destroyed");
}

