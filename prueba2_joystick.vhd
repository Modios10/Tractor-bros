library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity prueba2_joystick is
    Port (  
        boton        : in  std_logic;
        boton2       : in  std_logic;
        clk          : in  std_logic; -- 50MHz
        uart_tx      : out std_logic;
		   --declaracion de display de 7 segmentos
		  dB, dF, dA, dG, dC, dDP, dD, dE	: OUT std_logic
    );
end prueba2_joystick;

architecture rtl of prueba2_joystick is

    -- Componente del ADC (Sintaxis corregida)
    component adc0 is
        port (
            CLOCK : in  std_logic := 'X'; 
            RESET : in  std_logic := 'X'; 
            CH0   : out std_logic_vector(11 downto 0);
            CH1   : out std_logic_vector(11 downto 0);
            CH2   : out std_logic_vector(11 downto 0);
            CH3   : out std_logic_vector(11 downto 0);
            CH4   : out std_logic_vector(11 downto 0);
            CH5   : out std_logic_vector(11 downto 0);
            CH6   : out std_logic_vector(11 downto 0);
            CH7   : out std_logic_vector(11 downto 0)
        );
    end component adc0;

    signal btn1_clean, btn2_clean : std_logic;
    signal send_flag              : std_logic := '0';
    signal busy                   : std_logic;
    signal data_to_send           : std_logic_vector(7 downto 0);
    
    signal raw_x, raw_y           : std_logic_vector(11 downto 0);
    signal x_val_stored           : std_logic_vector(7 downto 0);
    signal y_val_stored           : std_logic_vector(7 downto 0);
    signal buttons_stored         : std_logic_vector(7 downto 0);
    
    signal timer_pulse            : std_logic;
    
    -- FSM Binaria para Unity
    type joy_state_type is (IDLE, SEND_HEADER, SEND_X, SEND_Y, SEND_BUTTONS);
    signal joy_state : joy_state_type := IDLE;

begin

    -- Instancia del ADC (Sintaxis de mapeo corregida)
    u0 : component adc0
        port map (
            CLOCK => clk,
            RESET => '0', 
            CH0   => raw_x,
            CH1   => raw_y,
            CH2   => open,
            CH3   => open,
            CH4   => open,
            CH5   => open,
            CH6   => open,
            CH7   => open
        );

    -- Periféricos
    DB1: entity work.debounce port map(clk => clk, sig_in => boton, sig_stable => btn1_clean);
    DB2: entity work.debounce port map(clk => clk, sig_in => boton2, sig_stable => btn2_clean);

    -- Timer a 50Hz (Pulso cada 1,000,000 de ciclos de 50MHz = 0.02 segundos)
    TIMER: entity work.timer_1sec 
        generic map(CLK_FREQ => 1_000_000) 
        port map(clk => clk, pulse => timer_pulse);

    TX_UNIT: entity work.uart_tx port map(clk => clk, start => send_flag, data_in => data_to_send, tx_line => uart_tx, busy => busy);
          
    process(clk)
    begin
        if rising_edge(clk) then
            led_azul <= not btn1_clean;
            led_rojo <= not btn2_clean;
            send_flag <= '0'; 

            if busy = '0' and send_flag = '0' then
                case joy_state is
                    
                    when IDLE =>
                        if timer_pulse = '1' then
                            x_val_stored <= raw_x(11 downto 4);
                            y_val_stored <= raw_y(11 downto 4);
                            -- Empaquetado de botones: Bit 0 = Boton 1, Bit 1 = Boton 2
                            buttons_stored <= "000000" & (not btn2_clean) & (not btn1_clean);
                            joy_state <= SEND_HEADER;
                        end if;

                    when SEND_HEADER =>
                        data_to_send <= x"FF"; -- 255 en decimal
                        send_flag    <= '1';
                        joy_state    <= SEND_X;

                    when SEND_X =>
                        data_to_send <= x_val_stored;
                        send_flag    <= '1';
                        joy_state    <= SEND_Y;

                    when SEND_Y =>
                        data_to_send <= y_val_stored;
                        send_flag    <= '1';
                        joy_state    <= SEND_BUTTONS;

                    when SEND_BUTTONS =>
                        data_to_send <= buttons_stored;
                        send_flag    <= '1';
                        joy_state    <= IDLE;

                    when others =>
                        joy_state <= IDLE;
                end case;
            end if;
        end if;
    end process;

end rtl;
